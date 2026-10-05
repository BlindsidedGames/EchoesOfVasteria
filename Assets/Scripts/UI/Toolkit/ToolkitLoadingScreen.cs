using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
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
        private void OnEnable()
        {
            if(!theme || !runtimeTheme || !textSettings)return;
            settings=ToolkitPanel.CreateSettings(runtimeTheme,textSettings);settings.sortingOrder=900;
            var document=GetComponent<UIDocument>(); document.panelSettings=settings; document.rootVisualElement.pickingMode=PickingMode.Ignore;
            root=new VisualElement();root.style.position=Position.Absolute;root.style.left=root.style.right=root.style.top=root.style.bottom=0;root.style.backgroundColor=new Color(.12f,.075f,.06f,.95f);root.style.alignItems=Align.Center;root.style.justifyContent=Justify.Center;theme.Apply(root);ToolkitGameplay.Apply(root,theme);document.rootVisualElement.Add(root);
            var box=new VisualElement();box.style.width=180;box.style.paddingTop=box.style.paddingBottom=box.style.paddingLeft=box.style.paddingRight=5;box.AddToClassList("surface");root.Add(box);
            loadingLabel=ToolkitControls.Text(ToolkitLocalization.Text("loading.title", "Loading..."),ToolkitControls.TextRole.Heading);box.Add(loadingLabel);ToolkitLocalization.Changed+=RefreshText;var track=new VisualElement();track.style.height=4;track.style.backgroundColor=new Color(.12f,.075f,.06f);box.Add(track);fill=new VisualElement();fill.style.height=4;fill.style.backgroundColor=new Color(.47f,.72f,.26f);track.Add(fill);
            Refresh();
        }
        private void RefreshText(){if(loadingLabel!=null)loadingLabel.text=ToolkitLocalization.Text("loading.title", "Loading...");}
        private void Update()=>Refresh();
        private void Refresh(){if(root==null)return;var manager=GameManager.Instance;bool loading=SceneManager.GetActiveScene().name=="Loading" || manager&&manager.RunLoadingActive;root.style.display=loading?DisplayStyle.Flex:DisplayStyle.None;fill.style.width=Length.Percent(manager?manager.RunLoadingFraction*100:0);}
        private void OnDisable(){ToolkitLocalization.Changed-=RefreshText;loadingLabel=null;root?.RemoveFromHierarchy();root=null;if(settings)Destroy(settings);settings=null;}
    }
}
