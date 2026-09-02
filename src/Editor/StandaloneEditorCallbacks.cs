using System;
using System.Windows.Forms;

namespace Editor
{
    /// <summary>
    /// Callback-функции, которые standalone-редактор может предоставить
    /// основной сборке для замены функциональности, завязанной на EPLAN.
    /// </summary>
    public static class StandaloneEditorCallbacks
    {
        /// <summary>
        /// Открыть окно выбора устройств.
        /// </summary>
        /// <param name="owner">Родительское окно.</param>
        /// <param name="currentValue">Текущее значение — строка устройств
        /// через пробел.</param>
        /// <returns>Новое значение или <c>null</c>, если выбор отменён.</returns>
        public static Func<IWin32Window, string, string> SelectDevices;
    }
}
