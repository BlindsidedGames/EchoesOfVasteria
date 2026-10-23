#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TimelessEchoes.Platform;
using UnityEditor;
using UnityEngine;
namespace Tests.EditMode {
 public sealed class ScreenshotRandomTests {
  [Test] public void NormalPlayUsesUnityRandomAndCapturePlacementIgnoresOtherRandomConsumers() {
   var flag=SessionState.GetBool("EOV.ScreenshotStudio.Active",false);var state=Random.state;
   try {
    SessionState.SetBool("EOV.ScreenshotStudio.Active",false);
    Random.InitState(77);var expected=Random.Range(0,100000);
    Random.InitState(77);EditorCaptureRandom.Reset(222);
    Assert.AreEqual(expected,EditorCaptureRandom.Range(0,100000),"Normal play must keep the original RNG contract.");
    SessionState.SetBool("EOV.ScreenshotStudio.Active",true);
    EditorCaptureRandom.Reset(104729);var sequence=new int[12];
    for(int i=0;i<sequence.Length;i++)sequence[i]=EditorCaptureRandom.Range(0,100000);
    EditorCaptureRandom.Reset(104729);
    for(int i=0;i<sequence.Length;i++){for(int n=0;n<15;n++)_ = Random.value;Assert.AreEqual(sequence[i],EditorCaptureRandom.Range(0,100000),"Animation or particle draws must not change screenshot task placement.");}
   } finally {Random.state=state;SessionState.SetBool("EOV.ScreenshotStudio.Active",flag);}
  }
 }
}
#endif
