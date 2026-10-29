using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InterprojectExchange;
using PInvoke;

namespace EasyEPlanner
{
    /// <summary>
    /// Описание параметра configuration.ini и подсказки к нему.
    /// </summary>
    public sealed class AddInSetting
    {
        public AddInSetting(string section, string key, string title,
            string description, string defaultValue = "",
            bool isBoolean = false, bool isFolder = false)
        {
            Section = section;
            Key = key;
            Title = title;
            Description = description;
            DefaultValue = defaultValue;
            IsBoolean = isBoolean;
            IsFolder = isFolder;
        }

        public string Section { get; }
        public string Key { get; }
        public string Title { get; }
        public string Description { get; }
        public string DefaultValue { get; }
        public bool IsBoolean { get; }
        public bool IsFolder { get; }
        public string Id => Section + "/" + Key;
    }

    /// <summary>
    /// Независимый от EPLAN черновик всех настроек надстройки.
    /// </summary>
    public sealed class AddInSettings
    {
        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>();

        public static IReadOnlyList<AddInSetting> Definitions { get; } =
            CreateDefinitions().AsReadOnly();

        public AddInSettings()
        {
            foreach (var setting in Definitions)
            {
                values[setting.Id] = setting.DefaultValue;
            }
        }

        public string this[AddInSetting setting]
        {
            get => values[setting.Id];
            set => values[setting.Id] = value ?? string.Empty;
        }

        public static AddInSettings Load(string path)
        {
            var settings = new AddInSettings();
            if (!File.Exists(path))
            {
                return settings;
            }

            string temporaryPath = Path.GetTempFileName();
            try
            {
                CopyAsUnicode(path, temporaryPath);
                var ini = new IniFile(temporaryPath);
                foreach (var setting in Definitions)
                {
                    string value = ini.ReadString(setting.Section,
                        setting.Key, setting.DefaultValue);
                    if (setting.IsBoolean)
                    {
                        value = bool.TryParse(value, out bool enabled)
                            ? enabled.ToString() : setting.DefaultValue;
                    }
                    settings[setting] = value;
                }
            }
            finally
            {
                File.Delete(temporaryPath);
            }
            return settings;
        }

        public void Save(string path)
        {
            foreach (var setting in Definitions)
            {
                Validate(setting, this[setting]);
            }

            path = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory,
                Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // Читаем актуальный файл: посторонние секции и комментарии
                // должны сохраниться, даже если изменились после открытия.
                CopyAsUnicode(path, temporaryPath);
                var ini = new IniFile(temporaryPath);
                foreach (var setting in Definitions)
                {
                    ini.WriteString(setting.Section, setting.Key,
                        Normalize(setting, this[setting]));
                }

                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        public static void Validate(AddInSetting setting, string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            {
                throw new ArgumentException("Значение должно занимать " +
                    "одну строку и не содержать нулевых символов.");
            }
            if (value.Length > 32000)
            {
                throw new ArgumentException("Значение слишком длинное.");
            }
            if (setting.IsBoolean && !bool.TryParse(value, out _))
            {
                throw new ArgumentException("Выберите True или False.");
            }
            if (setting.IsFolder && !string.IsNullOrWhiteSpace(value))
            {
                var paths = setting.Key == "folder_path"
                    ? value.Split(';') : new[] { value };
                foreach (string folder in paths)
                {
                    string normalized = folder.Trim().Trim('"');
                    if (normalized.Length == 0 ||
                        !Path.IsPathRooted(normalized) ||
                        Path.GetPathRoot(normalized).Length < 3 ||
                        normalized.IndexOfAny(
                            Path.GetInvalidPathChars()) >= 0 ||
                        normalized.IndexOfAny(new[] { '*', '?' }) >= 0)
                    {
                        throw new ArgumentException("Укажите полный путь " +
                            "к папке, например C:\\Projects или " +
                            "\\\\server\\Projects.");
                    }
                }
            }
            if (setting.Section == "helpSystem" &&
                !string.IsNullOrWhiteSpace(value))
            {
                if (!Uri.TryCreate(value.Trim(), UriKind.Absolute,
                    out Uri uri) || (uri.Scheme != Uri.UriSchemeHttp &&
                    uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("Укажите адрес, " +
                        "начинающийся с http:// или https://.");
                }
            }
        }

        private static string Normalize(AddInSetting setting, string value)
        {
            if (setting.IsBoolean)
            {
                return bool.Parse(value).ToString();
            }
            if (setting.IsFolder)
            {
                return setting.Key == "folder_path"
                    ? string.Join(";", value.Split(';')
                        .Select(folder => folder.Trim().Trim('"')))
                    : value.Trim().Trim('"');
            }
            return value.Trim();
        }

        private static void CopyAsUnicode(string source, string destination)
        {
            string text = string.Empty;
            if (File.Exists(source))
            {
                try
                {
                    // Распознаём UTF-8, BOM UTF-16 и старые ANSI-файлы.
                    using (var reader = new StreamReader(source,
                        new UTF8Encoding(false, true), true))
                    {
                        text = reader.ReadToEnd();
                    }
                }
                catch (DecoderFallbackException)
                {
                    text = File.ReadAllText(source, Encoding.Default);
                }
            }
            // UTF-16 с BOM поддерживается штатным INI API Windows.
            File.WriteAllText(destination, text, Encoding.Unicode);
        }

        private static List<AddInSetting> CreateDefinitions()
        {
            var settings = new List<AddInSetting>
            {
                new AddInSetting("path", "folder_path",
                    "Каталоги проектов",
                    "Корневые папки с Lua-проектами в порядке поиска. " +
                    "Разделяйте несколько путей точкой с запятой (;). " +
                    "Выбирается первая папка, содержащая нужный проект; " +
                    "новый проект сохраняется в первую указанную папку. " +
                    "Кнопка выбора добавляет папку в конец списка.",
                    isFolder: true),
                new AddInSetting("path", "wago_import_macros_path",
                    "Макросы WAGO для импорта",
                    "Папка специальных EPLAN-макросов модулей WAGO " +
                    "для импорта ICP CON. Поиск начинается с неё, " +
                    "затем продолжается в основном каталоге макросов.",
                    isFolder: true),
                new AddInSetting("path", "wago_macros_path",
                    "Основные макросы WAGO",
                    "Резервная папка EPLAN-макросов модулей WAGO. " +
                    "Макросы разбиваются по группам 750-x00 и " +
                    "именуются WAGO.750-xyy.ema.", isFolder: true),
                new AddInSetting("TestSettings", "ProjectTestEnabled",
                    "Тестировать проект при сохранении",
                    "Включает проверку работоспособности проекта. " +
                    "Требуется установленный Lua, доступный из " +
                    "командной строки. По умолчанию выключено.",
                    "False", isBoolean: true),
                new AddInSetting("helpSystem", "address",
                    "Адрес базы знаний OSTIS",
                    "Базовый HTTP(S)-адрес для формирования ссылок " +
                    "на описания технологических объектов в OSTIS. " +
                    "Можно оставить пустым, если OSTIS не используется."),
                new AddInSetting("helpSystem", "mainAddress",
                    "Главная страница справки",
                    "HTTP(S)-адрес страницы, открываемой, если " +
                    "описание объекта в OSTIS не найдено. " +
                    "Можно оставить пустым."),
                new AddInSetting("bindedSignalsList",
                    "groupAsPairsCheckBox", "Группировка по парам",
                    "Группирует связанные сигналы межпроектного " +
                    "обмена по парам устройств.", "False", true),
                new AddInSetting("bindedSignalsList",
                    "hideBindedSignalsCheckBox",
                    "Скрывать уже связанные сигналы",
                    "Убирает уже связанные сигналы из списков " +
                    "устройств обоих проектов.", "False", true),
                new AddInSetting("bindedSignalsList",
                    "disableCheckSignalsPairsCheckBox",
                    "Отключить контроль пар сигналов",
                    "Разрешает связывать сигналы без проверки " +
                    "соответствия типов DI ↔ DO и AI ↔ AO.",
                    "False", true),
            };
            foreach (string section in new[]
                { "currProjDevList", "advProjDevList" })
            {
                foreach (string type in
                    FilterConfiguration.GetSupportedDeviceTypes())
                {
                    bool enabled = new[] { "DI", "DO", "AI", "AO" }
                        .Contains(type);
                    settings.Add(new AddInSetting(section, type, type,
                        "Показывать устройства типа " + type +
                        " в списке сигналов. Если все типы сняты, " +
                        "отображаются все устройства.",
                        enabled.ToString(), true));
                }
            }
            return settings;
        }
    }
}
