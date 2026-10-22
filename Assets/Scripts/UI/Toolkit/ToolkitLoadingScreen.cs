using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using TimelessEchoes.Platform;
namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitLoadingScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, fill;
        private Label loadingLabel;
        private Label diagnosticLabel;
        private void OnEnable()
        {
            if(!theme || !runtimeTheme || !textSettings)return;
            settings=ToolkitPanel.CreateSettings(runtimeTheme,textSettings);settings.sortingOrder=900;
            var document=GetComponent<UIDocument>(); document.panelSettings=settings; document.rootVisualElement.pickingMode=PickingMode.Ignore;
            root=new VisualElement { name = "loading-backdrop" };root.style.position=Position.Absolute;root.style.left=root.style.top=0;root.AddToClassList("loading-screen");root.style.opacity=1;root.style.alignItems=Align.Center;root.style.justifyContent=Justify.Center;theme.Apply(root);ToolkitGameplay.Apply(root,theme);document.rootVisualElement.Add(root);
            var box=new VisualElement();box.style.width=180;box.style.paddingTop=box.style.paddingBottom=box.style.paddingLeft=box.style.paddingRight=5;box.AddToClassList("surface");root.Add(box);
            loadingLabel=ToolkitControls.Text(ToolkitLocalization.Text("loading.title", "Loading..."),ToolkitControls.TextRole.Heading);box.Add(loadingLabel);ToolkitLocalization.Changed+=RefreshText;var track=new VisualElement();track.style.height=4;track.AddToClassList("loading-track");box.Add(track);fill=new VisualElement();fill.style.height=4;fill.AddToClassList("loading-fill");track.Add(fill);
            if (StartupDiagnostics.Enabled)
            {
                box.style.width = Length.Percent(90);
                box.style.maxWidth = 440;
                box.style.paddingTop = box.style.paddingBottom = box.style.paddingLeft = box.style.paddingRight = 12;
                loadingLabel.style.whiteSpace = WhiteSpace.Normal;
                diagnosticLabel = ToolkitControls.Text("", ToolkitControls.TextRole.Body);
                diagnosticLabel.style.whiteSpace = WhiteSpace.Normal;
                diagnosticLabel.style.marginTop = 10;
                box.Add(diagnosticLabel);
            }
            Refresh();
        }
        private void RefreshText(){if(loadingLabel!=null)loadingLabel.text=ToolkitLocalization.Text("loading.title", "Loading...");}
        private void Update(){StartupDiagnostics.Heartbeat();Refresh();}
        private void Refresh()
        {
            if (root == null) return;
            if (StartupDiagnostics.Enabled) { root.style.display = DisplayStyle.None; return; }
            root.style.width = ToolkitResponsiveLayout.Size.x;
            root.style.height = ToolkitResponsiveLayout.Size.y;
            var manager = GameManager.Instance;
            bool startup = StartupDiagnostics.Enabled && StartupDiagnostics.Active;
            bool loading = startup || SceneManager.GetActiveScene().name == "Loading" || manager && manager.RunLoadingActive;
            root.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            fill.style.width = Length.Percent(startup ? StartupDiagnostics.Progress * 100 : manager ? manager.RunLoadingFraction * 100 : 0);
            if (diagnosticLabel == null) return;
            diagnosticLabel.style.display = startup ? DisplayStyle.Flex : DisplayStyle.None;
            if (startup)
            {
                loadingLabel.text = StartupDiagnostics.Phase;
                diagnosticLabel.text = $"Internal startup diagnostics • {Application.version}\n" + StartupDiagnostics.CurrentDetails;
                if (!string.IsNullOrEmpty(StartupDiagnostics.PreviousAttempt))
                    diagnosticLabel.text += "\n\n" + StartupDiagnostics.PreviousAttempt;
            }
            else RefreshText();
        }
        private void OnDisable(){ToolkitLocalization.Changed-=RefreshText;loadingLabel=null;diagnosticLabel=null;root?.RemoveFromHierarchy();root=null;if(settings)Destroy(settings);settings=null;}
    }
}
