using System.Globalization;

namespace Baseline.Config;

/// <summary>支持的界面语言。System = 跟随系统，其余按 enum 序号对应字符串表里的列。</summary>
public enum AppLanguage
{
    System = -1,
    English = 0,
    ChineseSimplified,
    ChineseTraditional,
    Japanese,
    Korean,
    Spanish,
    French,
    German,
    Russian,
    Portuguese,
}

/// <summary>
/// 轻量字符串表式 i18n：所有面向用户的文案集中在此，按 key 取当前语言译文。
/// 不用 .resx 卫星程序集——与单文件发布兼容差。切语言后触发 <see cref="Changed"/>，
/// 由托盘/进度条等监听者重建文案。
/// </summary>
public static class Loc
{
    /// <summary>支持的语言总数（不含 System），即字符串表每行数组长度。</summary>
    public const int Count = 10;

    /// <summary>当前生效语言（已解析，绝不为 System）。</summary>
    public static AppLanguage Current { get; private set; } = AppLanguage.English;

    /// <summary>语言切换后触发，供托盘菜单、进度条段名等实时重建。</summary>
    public static event Action? Changed;

    /// <summary>下拉框里展示的语言名（各语言用自身写法，与当前语言无关）。</summary>
    public static readonly (AppLanguage Lang, string Display)[] Choices =
    {
        (AppLanguage.System, "lang.system"), // 占位，显示时用 T() 取当前语言的「跟随系统」
        (AppLanguage.English, "English"),
        (AppLanguage.ChineseSimplified, "简体中文"),
        (AppLanguage.ChineseTraditional, "繁體中文"),
        (AppLanguage.Japanese, "日本語"),
        (AppLanguage.Korean, "한국어"),
        (AppLanguage.Spanish, "Español"),
        (AppLanguage.French, "Français"),
        (AppLanguage.German, "Deutsch"),
        (AppLanguage.Russian, "Русский"),
        (AppLanguage.Portuguese, "Português"),
    };

    /// <summary>设置语言（System 会按系统区域解析），变化时通知监听者。</summary>
    public static void SetLanguage(AppLanguage lang)
    {
        var resolved = lang == AppLanguage.System ? Resolve(CultureInfo.CurrentUICulture) : lang;
        if (resolved == Current) return;
        Current = resolved;
        Changed?.Invoke();
    }

    /// <summary>按 key 取当前语言译文，缺失则回退英文，再缺退 key 本身。</summary>
    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var row)) return key;
        int i = (int)Current;
        if (i >= 0 && i < row.Length && !string.IsNullOrEmpty(row[i])) return row[i];
        return string.IsNullOrEmpty(row[0]) ? key : row[0];
    }

    /// <summary>进度条段名：CPU/GPU 全语言通用，内存/带宽走字符串表。</summary>
    public static string SegLabel(MetricKind kind) => kind switch
    {
        MetricKind.Cpu => "CPU",
        MetricKind.Gpu => "GPU",
        MetricKind.Mem => T("seg.mem"),
        MetricKind.Net => T("seg.net"),
        _ => "",
    };

    private static AppLanguage Resolve(CultureInfo c)
    {
        // 中文按简繁细分，其余按两字母主语言归类
        if (c.TwoLetterISOLanguageName == "zh")
            return c.Name.Contains("Hant") || c.Name is "zh-TW" or "zh-HK" or "zh-MO"
                ? AppLanguage.ChineseTraditional
                : AppLanguage.ChineseSimplified;

        return c.TwoLetterISOLanguageName switch
        {
            "ja" => AppLanguage.Japanese,
            "ko" => AppLanguage.Korean,
            "es" => AppLanguage.Spanish,
            "fr" => AppLanguage.French,
            "de" => AppLanguage.German,
            "ru" => AppLanguage.Russian,
            "pt" => AppLanguage.Portuguese,
            _ => AppLanguage.English,
        };
    }

    // 每行 10 列，顺序严格对应 AppLanguage：
    // [en, zh-CN, zh-TW, ja, ko, es, fr, de, ru, pt]
    private static readonly Dictionary<string, string[]> Table = new()
    {
        ["settings.title"] = new[] { "Baseline Settings", "Baseline 设置", "Baseline 設定", "Baseline 設定", "Baseline 설정", "Configuración de Baseline", "Paramètres Baseline", "Baseline-Einstellungen", "Настройки Baseline", "Configurações do Baseline" },
        ["group.appearance"] = new[] { "Appearance", "外观", "外觀", "外観", "모양", "Apariencia", "Apparence", "Darstellung", "Внешний вид", "Aparência" },
        ["field.barHeight"] = new[] { "Bar height", "条高", "條高", "バーの高さ", "막대 높이", "Altura de barra", "Hauteur de barre", "Balkenhöhe", "Высота полосы", "Altura da barra" },
        ["field.opacity"] = new[] { "Opacity", "透明度", "透明度", "不透明度", "투명도", "Opacidad", "Opacité", "Deckkraft", "Прозрачность", "Opacidade" },
        ["field.refresh"] = new[] { "Refresh", "刷新", "刷新", "更新間隔", "새로 고침", "Frecuencia", "Actualisation", "Aktualisierung", "Обновление", "Atualização" },
        ["unit.sec"] = new[] { "s", "秒", "秒", "秒", "초", "s", "s", "s", "с", "s" },
        ["group.network"] = new[] { "Network", "网络", "網路", "ネットワーク", "네트워크", "Red", "Réseau", "Netzwerk", "Сеть", "Rede" },
        ["field.bandwidth"] = new[] { "Bandwidth cap", "宽带上限", "頻寬上限", "帯域上限", "대역폭 상한", "Límite de banda", "Limite de débit", "Bandbreitenlimit", "Лимит канала", "Limite de banda" },
        ["group.segments"] = new[] { "Segments shown", "显示哪些段", "顯示哪些段", "表示する項目", "표시할 항목", "Segmentos visibles", "Segments affichés", "Angezeigte Segmente", "Показывать сегменты", "Segmentos exibidos" },
        ["group.position"] = new[] { "Position", "位置", "位置", "位置", "위치", "Posición", "Position", "Position", "Положение", "Posição" },
        ["pos.bottom"] = new[] { "Bottom edge", "屏幕底边", "螢幕底邊", "画面下端", "화면 아래쪽", "Borde inferior", "Bord inférieur", "Unterer Rand", "Снизу", "Borda inferior" },
        ["pos.top"] = new[] { "Top edge", "屏幕顶边", "螢幕頂邊", "画面上端", "화면 위쪽", "Borde superior", "Bord supérieur", "Oberer Rand", "Сверху", "Borda superior" },
        ["field.monitor"] = new[] { "Monitor", "显示器", "顯示器", "モニター", "모니터", "Monitor", "Écran", "Monitor", "Монитор", "Monitor" },
        ["monitor.primary"] = new[] { "Primary monitor", "主显示器", "主顯示器", "メインモニター", "기본 모니터", "Monitor principal", "Écran principal", "Hauptmonitor", "Основной монитор", "Monitor principal" },
        ["monitor.secondary"] = new[] { "Monitor", "显示器", "顯示器", "モニター", "모니터", "Monitor", "Écran", "Monitor", "Монитор", "Monitor" },
        ["field.language"] = new[] { "Language", "语言", "語言", "言語", "언어", "Idioma", "Langue", "Sprache", "Язык", "Idioma" },
        ["field.autostart"] = new[] { "Start with Windows", "开机自启", "開機自啟", "Windows 起動時に開始", "Windows 시작 시 실행", "Iniciar con Windows", "Démarrer avec Windows", "Mit Windows starten", "Запуск с Windows", "Iniciar com o Windows" },
        ["btn.cancel"] = new[] { "Cancel", "取消", "取消", "キャンセル", "취소", "Cancelar", "Annuler", "Abbrechen", "Отмена", "Cancelar" },
        ["btn.ok"] = new[] { "OK", "确定", "確定", "OK", "확인", "Aceptar", "OK", "OK", "ОК", "OK" },
        ["tray.settings"] = new[] { "Settings…", "设置…", "設定…", "設定…", "설정…", "Configuración…", "Paramètres…", "Einstellungen…", "Настройки…", "Configurações…" },
        ["tray.restart"] = new[] { "Restart", "重启", "重啟", "再起動", "다시 시작", "Reiniciar", "Redémarrer", "Neu starten", "Перезапуск", "Reiniciar" },
        ["tray.exit"] = new[] { "Exit", "退出", "退出", "終了", "종료", "Salir", "Quitter", "Beenden", "Выход", "Sair" },
        ["tray.tooltip"] = new[] { "Baseline — resource bar", "Baseline 资源进度条", "Baseline 資源進度條", "Baseline リソースバー", "Baseline 리소스 표시줄", "Baseline — barra de recursos", "Baseline — barre de ressources", "Baseline — Ressourcenleiste", "Baseline — индикатор ресурсов", "Baseline — barra de recursos" },
        ["lang.system"] = new[] { "System default", "跟随系统", "跟隨系統", "システム既定", "시스템 기본값", "Predeterminado del sistema", "Par défaut du système", "Systemstandard", "Как в системе", "Padrão do sistema" },
        ["seg.mem"] = new[] { "RAM", "内存", "記憶體", "メモリ", "메모리", "RAM", "RAM", "RAM", "ОЗУ", "RAM" },
        ["seg.net"] = new[] { "Net", "带宽", "頻寬", "回線", "네트워크", "Red", "Réseau", "Netz", "Сеть", "Rede" },
    };
}
