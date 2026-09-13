using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RAXY.Utility.Editor.Hub;
using UnityEditor;
using UnityEngine;

namespace RAXY.Narrative
{
    public sealed class NarrativeHubModule : IRaxyHubModule
    {
        enum DialogueKind
        {
            Fullscreen,
            Banter
        }

        struct DialogueEntry
        {
            public DialogueKind Kind;
            public ScriptableObject Asset;
            public string AssetPath;
            public string FolderPath;
            public string DisplayName;
        }

        struct CutsceneEntry
        {
            public TimelineCutscene Cutscene;
            public string AssetPath;
            public string FolderPath;
            public string DisplayName;
        }

        enum ContentTab
        {
            FullscreenDialogue = 0,
            BanterDialogue = 1,
            TimelineCutscenes = 2
        }

        static readonly string[] ContentTabLabels =
        {
            "Fullscreen Dialogue",
            "Banter Dialogue",
            "Timeline Cutscenes"
        };

        readonly List<DialogueEntry> _dialogues = new();
        readonly List<CutsceneEntry> _cutscenes = new();
        readonly Dictionary<string, int> _collectionIndexByPath = new();
        readonly Dictionary<string, int> _timelineIndexByPath = new();

        ContentTab _contentTab = ContentTab.FullscreenDialogue;
        Vector2 _scroll;
        string _filter = "";

        public string Id => "narrative";
        public string DisplayName => "Narrative";
        public int Order => 110;

        public void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshAll();
        }

        public void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorWindow.HasOpenInstances<RaxyProjectHubWindow>())
                    EditorWindow.GetWindow<RaxyProjectHubWindow>().Repaint();
            };
        }

        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                DrawManagerSection();
                EditorGUILayout.Space(10f);

                _contentTab = (ContentTab)GUILayout.Toolbar((int)_contentTab, ContentTabLabels);
                EditorGUILayout.Space(8f);

                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

                DrawToolbar();
                EditorGUILayout.Space(6f);

                switch (_contentTab)
                {
                    case ContentTab.FullscreenDialogue:
                    case ContentTab.BanterDialogue:
                        DrawDialogueList();
                        break;
                    case ContentTab.TimelineCutscenes:
                        DrawCutsceneDefaultsSection();
                        EditorGUILayout.Space(10f);
                        DrawCutsceneSection();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }
        }

        void DrawManagerSection()
        {
            var hub = NarrativeHubManager.Instance;
            bool hasHub = hub != null;

            if (hasHub)
            {
                RaxyHubGui.DrawStatusBanner(
                    true,
                    "Runtime connected",
                    $"NarrativeHubManager.Instance = '{hub.name}'.");

                if (RaxyHubGui.PrimaryButton("Find Narrative Hub Instance"))
                    FindNarrativeHubInstance();

                EditorGUILayout.Space(4f);
                EditorGUILayout.ObjectField("Instance", hub, typeof(NarrativeHubManager), true);
            }
            else
            {
                RaxyHubGui.DrawStatusBanner(
                    false,
                    "Editor mode — hub not linked",
                    "Enter Play Mode with a NarrativeHubManager in the scene, then Find Instance to enable Play/End.");

                if (RaxyHubGui.PrimaryButton("Find Narrative Hub Instance"))
                    FindNarrativeHubInstance();
            }
        }

        void DrawCutsceneDefaultsSection()
        {
            EditorGUILayout.LabelField("Cutscene Defaults", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Default Editor Helper Prefabs are assigned automatically when creating a Cutscene Timeline via Assets/Create.",
                MessageType.None);

            var settings = NarrativeEditorSettings.instance;
            var so = new SerializedObject(settings);
            so.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                so.FindProperty("defaultEditorHelperPrefabs"),
                new GUIContent("Default Helper Prefabs"),
                true);

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                settings.SaveDefaults();
            }
            else
            {
                so.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(_cutscenes.Count == 0))
            {
                if (RaxyHubGui.PrimaryButton("Apply Defaults to All Cutscenes"))
                    ApplyDefaultHelpersToAllCutscenes();
            }
        }

        void ApplyDefaultHelpersToAllCutscenes()
        {
            RefreshCutscenes();

            int total = _cutscenes.Count;
            if (total == 0)
            {
                EditorUtility.DisplayDialog(
                    "Apply Default Helpers",
                    "No TimelineCutscene prefabs found in the project.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Apply Default Helpers",
                $"Overwrite Editor Helper Prefabs on all {total} Timeline Cutscene(s) in the list?\n\n" +
                "Existing helper prefab lists will be replaced with the Default Helper Prefabs.",
                "Apply",
                "Cancel");

            if (!confirmed)
                return;

            var defaults = NarrativeEditorSettings.instance.DefaultEditorHelperPrefabs;
            int updated = 0;

            for (int i = 0; i < _cutscenes.Count; i++)
            {
                var cutscene = _cutscenes[i].Cutscene;
                if (cutscene == null)
                    continue;

                Undo.RecordObject(cutscene, "Apply Default Editor Helpers");
                cutscene.ApplyEditorHelperPrefabs(defaults);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[NarrativeHub] Applied Default Helper Prefabs to {updated}/{total} Timeline Cutscene(s).");
        }

        void DrawToolbar()
        {
            _filter = RaxyHubGui.DrawToolbarRow(_filter, out bool refresh);
            if (refresh)
                RefreshAll();

            string countLabel;
            if (TryGetActiveDialogueKind(out DialogueKind kind))
            {
                int count = 0;
                for (int i = 0; i < _dialogues.Count; i++)
                {
                    if (_dialogues[i].Kind == kind)
                        count++;
                }

                countLabel = kind == DialogueKind.Fullscreen
                    ? $"{count} fullscreen"
                    : $"{count} banter";
            }
            else
            {
                countLabel = $"{_cutscenes.Count} cutscene prefabs";
            }

            RaxyHubGui.DrawCountChip(countLabel);
        }

        void DrawDialogueList()
        {
            if (!TryGetActiveDialogueKind(out DialogueKind kind))
                return;

            int kindTotal = 0;
            for (int i = 0; i < _dialogues.Count; i++)
            {
                if (_dialogues[i].Kind == kind)
                    kindTotal++;
            }

            if (kindTotal == 0)
            {
                string missing = kind == DialogueKind.Fullscreen
                    ? "No FullscreenDialogueDataSO found."
                    : "No BanterDialogueDataSO found.";
                EditorGUILayout.HelpBox(
                    missing + "\nCreate via Assets > Create > RAXY > Narrative.",
                    MessageType.Info);
                return;
            }

            bool hasHub = NarrativeHubManager.Instance != null;

            foreach (var entry in _dialogues)
            {
                if (entry.Asset == null || entry.Kind != kind)
                    continue;

                if (!PassesFilter(entry))
                    continue;

                DrawDialogueRow(entry, hasHub);
            }

            if (!hasHub)
                RaxyHubGui.DrawHint("Play / End disabled until Narrative Hub is linked.");
        }

        void DrawDialogueRow(DialogueEntry entry, bool hasHub)
        {
            RaxyHubGui.BeginCard();
            RaxyHubGui.DrawTitleRow(entry.DisplayName);
            RaxyHubGui.DrawMutedPath(entry.FolderPath);

            if (entry.Kind == DialogueKind.Fullscreen &&
                entry.Asset is FullscreenDialogueDataSO fullscreen)
            {
                EditorGUILayout.Space(2f);
                DrawCollectionDropdown(entry.AssetPath, fullscreen);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (RaxyHubGui.SecondaryButton("Ping", 56f))
                {
                    EditorGUIUtility.PingObject(entry.Asset);
                    Selection.activeObject = entry.Asset;
                }

                using (new EditorGUI.DisabledScope(!hasHub))
                {
                    if (RaxyHubGui.PrimaryButton("Play"))
                        PlayDialogue(entry);

                    if (RaxyHubGui.SecondaryButton("End", 56f))
                        EndDialogue(entry.Kind);
                }
            }

            RaxyHubGui.EndCard();
        }

        void DrawCollectionDropdown(string assetPath, FullscreenDialogueDataSO data)
        {
            if (data.dialogueCollections == null || data.dialogueCollections.Count == 0)
            {
                RaxyHubGui.DrawMutedPath("Collections: (none — Play uses default)");
                return;
            }

            var ids = data.CollectionIds;
            if (ids == null || ids.Count == 0)
            {
                RaxyHubGui.DrawMutedPath("Collections: (none — Play uses default)");
                return;
            }

            if (!_collectionIndexByPath.TryGetValue(assetPath, out int index))
                index = 0;

            index = Mathf.Clamp(index, 0, ids.Count - 1);
            var labels = ids.Select(id => string.IsNullOrEmpty(id) ? "(empty id)" : id).ToArray();
            int newIndex = EditorGUILayout.Popup("Collection", index, labels);
            _collectionIndexByPath[assetPath] = newIndex;
        }

        void DrawCutsceneSection()
        {
            bool hasHub = NarrativeHubManager.Instance != null;

            if (_cutscenes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No TimelineCutscene prefabs found. Create a prefab with a TimelineCutscene component.",
                    MessageType.Info);
                return;
            }

            foreach (var entry in _cutscenes)
            {
                if (entry.Cutscene == null)
                    continue;

                if (!PassesCutsceneFilter(entry))
                    continue;

                DrawCutsceneRow(entry, hasHub);
            }

            if (!hasHub)
                RaxyHubGui.DrawHint("Cutscene Play disabled until Narrative Hub is linked.");
        }

        void DrawCutsceneRow(CutsceneEntry entry, bool hasHub)
        {
            var cutscene = entry.Cutscene;
            var timelineIds = cutscene.TimelineIds?.ToList() ?? new List<string>();

            RaxyHubGui.BeginCard();
            RaxyHubGui.DrawTitleRow(entry.DisplayName, "Prefab");
            RaxyHubGui.DrawMutedPath(entry.FolderPath);

            string timelineId = null;
            EditorGUILayout.Space(2f);
            if (timelineIds.Count > 0)
            {
                if (!_timelineIndexByPath.TryGetValue(entry.AssetPath, out int index))
                    index = 0;

                index = Mathf.Clamp(index, 0, timelineIds.Count - 1);
                int newIndex = EditorGUILayout.Popup("Timeline Id", index, timelineIds.ToArray());
                _timelineIndexByPath[entry.AssetPath] = newIndex;
                timelineId = timelineIds[newIndex];
            }
            else
            {
                RaxyHubGui.DrawMutedPath("Timeline Id: (none registered)");
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (RaxyHubGui.SecondaryButton("Ping", 56f))
                {
                    var prefabRoot = cutscene.gameObject;
                    EditorGUIUtility.PingObject(prefabRoot);
                    Selection.activeObject = prefabRoot;
                }

                using (new EditorGUI.DisabledScope(!hasHub || string.IsNullOrEmpty(timelineId)))
                {
                    if (RaxyHubGui.PrimaryButton("Play"))
                        PlayCutscene(cutscene, timelineId);
                }
            }

            RaxyHubGui.EndCard();
        }

        bool PassesFilter(DialogueEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;

            string q = _filter.Trim();
            return entry.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.FolderPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool TryGetActiveDialogueKind(out DialogueKind kind)
        {
            switch (_contentTab)
            {
                case ContentTab.FullscreenDialogue:
                    kind = DialogueKind.Fullscreen;
                    return true;
                case ContentTab.BanterDialogue:
                    kind = DialogueKind.Banter;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        bool PassesCutsceneFilter(CutsceneEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;

            string q = _filter.Trim();
            return entry.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.FolderPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                   || entry.AssetPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void RefreshAll()
        {
            RefreshDialogues();
            RefreshCutscenes();
        }

        void RefreshDialogues()
        {
            _dialogues.Clear();

            AddDialogues("t:FullscreenDialogueDataSO", DialogueKind.Fullscreen);
            AddDialogues("t:BanterDialogueDataSO", DialogueKind.Banter);

            _dialogues.Sort((a, b) =>
            {
                int kind = a.Kind.CompareTo(b.Kind);
                if (kind != 0)
                    return kind;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        void AddDialogues(string filter, DialogueKind kind)
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;

                _dialogues.Add(new DialogueEntry
                {
                    Kind = kind,
                    Asset = asset,
                    AssetPath = path,
                    FolderPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? path,
                    DisplayName = asset.name
                });
            }
        }

        void RefreshCutscenes()
        {
            _cutscenes.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    continue;

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                    continue;

                var cutscene = root.GetComponentInChildren<TimelineCutscene>(true);
                if (cutscene == null)
                    continue;

                // Skip prefab instances that somehow aren't assets.
                if (!PrefabUtility.IsPartOfPrefabAsset(cutscene))
                    continue;

                _cutscenes.Add(new CutsceneEntry
                {
                    Cutscene = cutscene,
                    AssetPath = path,
                    FolderPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? path,
                    DisplayName = root.name
                });
            }

            _cutscenes.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        }

        void FindNarrativeHubInstance()
        {
            var found = UnityEngine.Object.FindObjectsByType<NarrativeHubManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (found == null || found.Length == 0)
            {
                Debug.LogWarning(
                    "[RAXY Hub / Narrative] No NarrativeHubManager found in loaded scenes. " +
                    "Enter Play Mode and ensure a NarrativeHubManager is present.");
                return;
            }

            if (found.Length > 1)
            {
                Debug.LogWarning(
                    $"[RAXY Hub / Narrative] Found {found.Length} NarrativeHubManager instances; " +
                    $"Singleton Instance is '{(NarrativeHubManager.Instance != null ? NarrativeHubManager.Instance.name : "null")}'.");
            }

            var hub = NarrativeHubManager.Instance != null ? NarrativeHubManager.Instance : found[0];
            EditorGUIUtility.PingObject(hub);
            Selection.activeGameObject = hub.gameObject;
            Debug.Log($"[RAXY Hub / Narrative] Found NarrativeHubManager '{hub.name}'.");
            EditorWindow.GetWindow<RaxyProjectHubWindow>()?.Repaint();
        }

        void PlayDialogue(DialogueEntry entry)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[RAXY Hub / Narrative] NarrativeHubManager.Instance is null.");
                return;
            }

            switch (entry.Kind)
            {
                case DialogueKind.Fullscreen:
                    if (entry.Asset is not FullscreenDialogueDataSO fullscreen)
                        return;

                    string collectionId = null;
                    if (fullscreen.dialogueCollections != null && fullscreen.dialogueCollections.Count > 0)
                    {
                        var ids = fullscreen.CollectionIds;
                        if (ids != null && ids.Count > 0 &&
                            _collectionIndexByPath.TryGetValue(entry.AssetPath, out int index))
                        {
                            index = Mathf.Clamp(index, 0, ids.Count - 1);
                            collectionId = ids[index];
                        }
                    }

                    hub.PlayFullscreenDialogue(fullscreen, collectionId);
                    break;

                case DialogueKind.Banter:
                    if (entry.Asset is BanterDialogueDataSO banter)
                        hub.PlayBanterDialogue(banter);
                    break;
            }
        }

        void EndDialogue(DialogueKind kind)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[RAXY Hub / Narrative] NarrativeHubManager.Instance is null.");
                return;
            }

            switch (kind)
            {
                case DialogueKind.Fullscreen:
                    hub.EndFullscreenDialogue();
                    break;
                case DialogueKind.Banter:
                    hub.EndBanterDialogue();
                    break;
            }
        }

        void PlayCutscene(TimelineCutscene cutscene, string timelineId)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[RAXY Hub / Narrative] NarrativeHubManager.Instance is null.");
                return;
            }

            if (string.IsNullOrEmpty(timelineId))
            {
                Debug.LogWarning("[RAXY Hub / Narrative] Timeline Id is empty.");
                return;
            }

            hub.PlayTimelineCutscene(cutscene, timelineId);
        }
    }
}
