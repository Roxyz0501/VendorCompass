using Dalamud.Game;

namespace VendorCompass;

public static class Loc
{
    public static readonly (string Code, string Native)[] Languages =
    [
        ("ja", "日本語"), ("en", "English"), ("de", "Deutsch"), ("fr", "Français"),
        ("ko", "한국어"), ("zh-Hans", "简体中文"), ("zh-Hant", "繁體中文")
    ];

    private static readonly Dictionary<string, string[]> Text = new()
    {
        ["Find"] = ["販売NPCを探す", "Find vendors", "Händler suchen", "Trouver les vendeurs", "판매 NPC 찾기", "查找商人", "尋找商人"],
        ["Vendors"] = ["販売NPC", "Vendors", "Händler", "Vendeurs", "판매 NPC", "商人", "商人"],
        ["Map"] = ["地図", "Map", "Karte", "Carte", "지도", "地图", "地圖"],
        ["Go"] = ["移動", "Travel", "Reisen", "Voyager", "이동", "前往", "前往"],
        ["Menu"] = ["右クリックメニューに表示", "Show in item context menus", "Im Gegenstandsmenü anzeigen", "Afficher dans le menu des objets", "아이템 메뉴에 표시", "在物品菜单中显示", "在物品選單中顯示"],
        ["Hint"] = ["アイテムを右クリックして「販売NPCを探す」を選んでください。", "Right-click an item and choose Find vendors.", "Klicke mit der rechten Maustaste auf einen Gegenstand und wähle „Händler suchen“.", "Faites un clic droit sur un objet et choisissez « Trouver les vendeurs ».", "아이템을 우클릭한 후 판매 NPC 찾기를 선택하세요.", "右键点击物品，选择“查找商人”。", "右鍵點選物品，選擇「尋找商人」。"],
        ["Stop"] = ["移動を中止", "Cancel travel", "Reise abbrechen", "Annuler le trajet", "이동 취소", "取消移动", "取消移動"],
        ["Stopped"] = ["移動を中止しました。", "Travel cancelled.", "Reise abgebrochen.", "Trajet annulé.", "이동을 취소했습니다.", "已取消移动。", "已取消移動。"],
        ["Loading"] = ["販売場所を読み込み中…", "Loading vendor locations…", "Händlerstandorte werden geladen…", "Chargement des emplacements…", "판매 위치를 불러오는 중…", "正在加载商人位置…", "正在載入商人位置…"],
        ["DataError"] = ["販売データを読み込めませんでした。", "Could not load vendor data.", "Händlerdaten konnten nicht geladen werden.", "Impossible de charger les vendeurs.", "판매 데이터를 불러올 수 없습니다.", "无法加载商人数据。", "無法載入商人資料。"],
        ["Unavailable"] = ["今は移動を開始できません。", "Travel is unavailable right now.", "Reisen ist momentan nicht möglich.", "Déplacement impossible pour le moment.", "지금은 이동할 수 없습니다.", "现在无法移动。", "現在無法移動。"],
        ["NeedNav"] = ["自動移動にはvnavmeshが必要です。「地図」で場所を確認できます。", "Enable vnavmesh to travel automatically, or select Map.", "Aktiviere vnavmesh für die automatische Reise oder wähle Karte.", "Activez vnavmesh pour le trajet automatique ou choisissez Carte.", "자동 이동하려면 vnavmesh를 활성화하세요. 지도로 위치를 볼 수 있습니다.", "请启用 vnavmesh 自动移动，或选择地图。", "請啟用 vnavmesh 自動移動，或選擇地圖。"],
        ["Busy"] = ["別の経路で移動中です。", "Another route is active.", "Eine andere Route ist aktiv.", "Un autre trajet est en cours.", "다른 경로로 이동 중입니다.", "其他路线正在使用中。", "其他路線正在使用中。"],
        ["Locked"] = ["このエリアに直接テレポできません。エリア到着後に「移動」を選んでください。", "No direct teleport is available. Enter the area and select Travel.", "Kein direkter Teleport verfügbar. Betritt das Gebiet und wähle Reisen.", "Aucune téléportation directe. Entrez dans la zone et choisissez Voyager.", "직접 텔레포할 수 없습니다. 지역에 진입한 후 이동을 선택하세요.", "无法直接传送。进入该区域后选择前往。", "無法直接傳送。進入該區域後選擇前往。"],
        ["Teleporting"] = ["テレポ先への到着を待っています…", "Waiting for teleport arrival…", "Warte auf Teleportankunft…", "Téléportation en cours…", "텔레포 도착 대기 중…", "正在等待传送完成…", "正在等候傳送完成…"],
        ["Planning"] = ["徒歩経路を探索中…", "Preparing walking route…", "Fußweg wird berechnet…", "Calcul de l’itinéraire à pied…", "도보 경로 탐색 중…", "正在规划步行路线…", "正在規劃步行路線…"],
        ["Walking"] = ["販売NPCへ移動中…", "Walking to the vendor…", "Unterwegs zum Händler…", "En route vers le vendeur…", "판매 NPC에게 이동 중…", "正在前往商人处…", "正在前往商人處…"],
        ["Arrived"] = ["販売NPCに到着しました。", "You have reached the vendor.", "Händler erreicht.", "Vous êtes arrivé auprès du vendeur.", "판매 NPC에게 도착했습니다.", "已到达商人处。", "已抵達商人處。"],
        ["Absent"] = ["販売場所に到着しましたが、NPCが見つかりません。", "Reached the location. The vendor is absent.", "Standort erreicht. Der Händler ist abwesend.", "Emplacement atteint. Le vendeur est absent.", "판매 위치에 도착했지만 NPC가 없습니다.", "已到达销售地点，未找到商人。", "已抵達販售地點，未找到商人。"],
        ["NoPath"] = ["NPCの近くまで歩ける経路が見つかりません。", "No walking route reaches this vendor.", "Kein Fußweg zum Händler gefunden.", "Aucun chemin vers ce vendeur.", "NPC까지 걸어갈 수 있는 경로가 없습니다.", "找不到通往该商人的步行路线。", "找不到通往該商人的步行路線。"],
        ["Interrupted"] = ["移動が中断されたか、時間切れになりました。", "Travel was interrupted or timed out.", "Reise unterbrochen oder Zeitlimit erreicht.", "Trajet interrompu ou délai dépassé.", "이동이 중단되었거나 시간이 초과되었습니다.", "移动已中断或超时。", "移動已中斷或逾時。"],
        ["SavePolicy"] = ["設定は60秒ごとに変更時だけ保存し、ログアウト・正常終了時にも保存します。", "Changed settings save every 60 seconds and on logout or normal shutdown.", "Geänderte Einstellungen werden alle 60 Sekunden und beim Abmelden oder regulären Beenden gespeichert.", "Les modifications sont enregistrées toutes les 60 secondes, à la déconnexion et à la fermeture normale.", "변경된 설정은 60초마다, 로그아웃 및 정상 종료 시 저장됩니다.", "设置仅在有更改时每60秒保存，并在登出或正常退出时保存。", "設定僅在有變更時每60秒儲存，並在登出或正常結束時儲存。"],
        ["Recovered"] = ["破損した設定を正常なバックアップから復元しました。", "Settings were recovered from a valid backup.", "Einstellungen wurden aus einer gültigen Sicherung wiederhergestellt.", "Les paramètres ont été restaurés depuis une sauvegarde valide.", "정상 백업에서 설정을 복구했습니다.", "已从有效备份恢复设置。", "已從有效備份還原設定。"],
        ["ResetConfig"] = ["設定を読み取れず、正常なバックアップもありません。初期値を使用します。破損ファイルは保存時に退避します。", "Settings and backups were invalid. Defaults are in use; the damaged file will be preserved when saving.", "Einstellungen und Sicherungen waren ungültig. Standardwerte werden verwendet; die beschädigte Datei bleibt beim Speichern erhalten.", "Paramètres et sauvegardes invalides : valeurs par défaut utilisées. Le fichier endommagé sera conservé lors de l’enregistrement.", "설정과 백업이 손상되어 기본값을 사용합니다. 저장 시 손상 파일을 보존합니다.", "设置和备份均无效，现使用默认值。保存时将保留损坏文件。", "設定與備份皆無效，目前使用預設值。儲存時將保留損毀檔案。"],
        ["BackupCleanup"] = ["設定は保存できましたが、古いバックアップの整理に失敗しました。ファイルのアクセス権を確認してください。", "Settings saved, but old backups could not be removed. Check file permissions.", "Gespeichert, aber alte Sicherungen konnten nicht entfernt werden. Dateiberechtigungen prüfen.", "Enregistrement réussi, mais suppression des anciennes sauvegardes impossible. Vérifiez les permissions.", "설정은 저장했지만 오래된 백업을 삭제하지 못했습니다. 파일 권한을 확인하세요.", "设置已保存，但无法清理旧备份。请检查文件权限。", "設定已儲存，但無法清理舊備份。請檢查檔案權限。"],
        ["LoadBlocked"] = ["設定ファイルにアクセスできないため保存を停止しています。原因を解消してプラグインを再読み込みしてください。", "Saving is blocked because settings could not be accessed. Resolve the error and reload the plugin.", "Speichern gesperrt: kein Zugriff auf Einstellungen. Fehler beheben und Plugin neu laden.", "Enregistrement bloqué : paramètres inaccessibles. Corrigez l’erreur puis rechargez le plugin.", "설정 파일에 접근할 수 없어 저장을 중지했습니다. 원인을 해결한 후 플러그인을 다시 불러오세요.", "无法访问设置文件，已停止保存。请排除故障后重新加载插件。", "無法存取設定檔案，已停止儲存。請排除故障後重新載入外掛。"],
        ["SaveFailed"] = ["設定を保存できませんでした。未保存の変更は保持しています。60秒以上空けて再試行します（連続3回まで）。", "Settings could not be saved. Changes remain pending; retries are at least 60 seconds apart, up to three failures.", "Speichern fehlgeschlagen. Änderungen bleiben vorgemerkt; Wiederholungen frühestens nach 60 Sekunden, maximal drei Fehler.", "Échec de l’enregistrement. Modifications en attente ; essais espacés d’au moins 60 secondes, arrêt après trois échecs.", "설정을 저장하지 못했습니다. 변경 사항은 유지되며 60초 이상 간격으로 최대 3회 실패까지 재시도합니다.", "设置保存失败，未保存的更改仍被保留。重试间隔至少60秒，连续失败3次后停止。", "設定儲存失敗，未儲存的變更仍被保留。重試間隔至少60秒，連續失敗3次後停止。"],
        ["SaveStopped"] = ["保存が3回失敗したため自動保存を停止しました。原因を解消して「保存を再試行」を押してください。終了すると未保存の変更は失われます。", "Autosave stopped after three failures. Resolve the error and retry saving. Pending changes will be lost on exit.", "Automatisches Speichern nach drei Fehlern gestoppt. Fehler beheben und erneut speichern. Ungespeicherte Änderungen gehen beim Beenden verloren.", "Enregistrement automatique arrêté après trois échecs. Corrigez l’erreur puis réessayez. Les modifications en attente seront perdues à la fermeture.", "3회 실패하여 자동 저장을 중지했습니다. 원인을 해결하고 저장을 재시도하세요. 종료하면 미저장 변경 사항은 사라집니다.", "保存连续失败3次，自动保存已停止。请排除故障后重试。退出时未保存的更改将丢失。", "儲存連續失敗3次，自動儲存已停止。請排除故障後重試。結束時未儲存的變更將遺失。"],
        ["RetrySave"] = ["保存を再試行", "Retry saving", "Speichern wiederholen", "Réessayer l’enregistrement", "저장 재시도", "重试保存", "重試儲存"],
        ["Settings"] = ["設定", "Settings", "Einstellungen", "Paramètres", "설정", "设置", "設定"],
        ["LanguageSettings"] = ["言語設定", "Language settings", "Spracheinstellungen", "Paramètres de langue", "언어 설정", "语言设置", "語言設定"],
        ["Support"] = ["支援", "Support", "Unterstützen", "Soutien", "후원", "支持", "支援"],
        ["Language"] = ["言語 / Language", "Language / 言語", "Sprache / Language", "Langue / Language", "언어 / Language", "语言 / Language", "語言 / Language"],
        ["LanguageHelp"] = ["表示言語を選択します。変更はすぐに反映され、次回起動後も保持されます。", "Choose the display language. Changes apply immediately and remain after restarting.", "Wähle die Anzeigesprache. Die Änderung gilt sofort und bleibt nach dem Neustart erhalten.", "Choisissez la langue d’affichage. Le changement s’applique aussitôt et reste enregistré après le redémarrage.", "표시 언어를 선택하세요. 변경 사항은 즉시 적용되며 다시 시작해도 유지됩니다.", "选择显示语言。更改会立即生效，并在重新启动后保留。", "選擇顯示語言。變更會立即生效，重新啟動後仍會保留。"],
        ["SupportText"] = ["Roxyz0501 を任意で支援できます。", "You can optionally support Roxyz0501.", "Du kannst Roxyz0501 freiwillig unterstützen.", "Vous pouvez soutenir Roxyz0501 si vous le souhaitez.", "원한다면 Roxyz0501을 후원할 수 있습니다.", "你可以自愿支持 Roxyz0501。", "你可以自願支持 Roxyz0501。"],
        ["SupportHeading"] = ["任意の支援", "Optional support", "Freiwillige Unterstützung", "Soutien facultatif", "선택적 후원", "自愿支持", "自願支持"],
        ["SupportRecipient"] = ["支援先: Roxyz0501", "Recipient: Roxyz0501", "Empfänger: Roxyz0501", "Bénéficiaire : Roxyz0501", "후원 받는 사람: Roxyz0501", "收款人：Roxyz0501", "受款人：Roxyz0501"],
        ["OpenKoFi"] = ["Ko-fi を開く", "Open Ko-fi", "Ko-fi öffnen", "Ouvrir Ko-fi", "Ko-fi 열기", "打开 Ko-fi", "開啟 Ko-fi"],
    };

    public static IEnumerable<string> AllStrings => Text.Values.SelectMany(x => x);
    public static string FontText => string.Concat(AllStrings) + string.Concat(Languages.Select(x => x.Native))
        + "—…\u202F\u00A0";
    public static ClientLanguage GameDataLanguage(string? code) => code switch
    {
        "ja" => ClientLanguage.Japanese,
        "de" => ClientLanguage.German,
        "fr" => ClientLanguage.French,
        _ => ClientLanguage.English,
    };

    public static bool IsSupported(string? code) => Languages.Any(x => x.Code == code);
    public static string? FromGame(ClientLanguage? value) => value switch
    {
        ClientLanguage.Japanese => "ja",
        ClientLanguage.English => "en",
        ClientLanguage.German => "de",
        ClientLanguage.French => "fr",
        _ => null,
    };

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = raw.Trim().Replace('_', '-').ToLowerInvariant();
        if (value is "ja" or "ja-jp") return "ja";
        if (value is "en" or "en-us" or "en-gb") return "en";
        if (value is "de" or "de-de") return "de";
        if (value is "fr" or "fr-fr") return "fr";
        if (value is "ko" or "ko-kr") return "ko";
        if (value is "zh-hans" or "zh-cn" or "zh-sg") return "zh-Hans";
        if (value is "zh-hant" or "zh-tw" or "zh-hk" or "zh-mo") return "zh-Hant";
        return null;
    }

    public static string Get(string? code, string key)
    {
        if (!Text.TryGetValue(key, out var values)) return key;
        var index = Array.FindIndex(Languages, x => x.Code == code);
        return values[index < 0 ? 1 : index];
    }

    public static void CheckCompleteness()
    {
        foreach (var (key, values) in Text)
        {
            if (values.Length != Languages.Length || values.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"Incomplete localization: {key}");
            var expected = CountParameters(values[1]);
            if (values.Any(x => CountParameters(x) != expected))
                throw new InvalidOperationException($"Localization format mismatch: {key}");
        }
    }

    private static int CountParameters(string value)
        => System.Text.RegularExpressions.Regex.Matches(value, @"\{\d+\}").Count;
}
