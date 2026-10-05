#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.EditMode
{
    public sealed class MainSceneIntegrityTests
    {
        private static readonly Regex DocumentHeader = new Regex(
            @"^--- !u!(?<type>\d+) &(?<id>-?\d+)(?<stripped> stripped)?\r?$",
            RegexOptions.Multiline);

        [TestCase("Assets/Scenes/Main.unity")]
        [TestCase("Assets/Scenes/Loading.unity")]
        public void AuthoredGameObjectsOwnExactlyOneSerializedTransform(string scenePath)
        {
            var text = File.ReadAllText(scenePath);
            var headers = DocumentHeader.Matches(text).Cast<Match>().ToArray();
            var documents = headers.Select((header, index) => new
            {
                Id = header.Groups["id"].Value,
                Type = int.Parse(header.Groups["type"].Value),
                Stripped = header.Groups["stripped"].Success,
                Text = text.Substring(header.Index,
                    (index + 1 < headers.Length ? headers[index + 1].Index : text.Length) - header.Index)
            }).ToDictionary(document => document.Id);
            var failures = new List<string>();
            foreach (var gameObject in documents.Values.Where(document => document.Type == 1 && !document.Stripped))
            {
                var components = Regex.Matches(gameObject.Text, @"^  - component: \{fileID: (-?\d+)\}", RegexOptions.Multiline)
                    .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
                var transforms = components.Where(id => documents.ContainsKey(id)
                    && (documents[id].Type == 4 || documents[id].Type == 224)).ToArray();
                var name = Regex.Match(gameObject.Text, @"^  m_Name: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
                if (transforms.Length != 1)
                {
                    failures.Add($"{name} ({gameObject.Id}) owns {transforms.Length} serialized Transforms.");
                    continue;
                }
                var owner = Regex.Match(documents[transforms[0]].Text,
                    @"^  m_GameObject: \{fileID: (-?\d+)\}", RegexOptions.Multiline).Groups[1].Value;
                if (owner != gameObject.Id)
                    failures.Add($"{name} ({gameObject.Id}) lists a Transform owned by {owner}.");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void MainLoadsWithoutSceneErrorsOrAutomaticTransformRepairs()
        {
            // Never close or reload a Main scene already owned by the user's Editor.
            var existingScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).ToArray();
            Assert.That(existingScenes.All(scene => scene.path != "Assets/Scenes/Main.unity"), Is.True,
                "Run the Main load check in a disposable test project with Main closed.");
            var existingHandles = new HashSet<SceneHandle>(existingScenes.Select(scene => scene.handle));
            var errors = new List<string>();
            Application.LogCallback collect = (message, trace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert
                    || message.Contains("Transform component could not be found")
                    || message.Contains("does not have a RectTransform")
                    || message.Contains("Creating missing RectTransform component"))
                    errors.Add(message);
            };
            var priorActive = SceneManager.GetActiveScene();
            var loaded = default(Scene);
            Application.logMessageReceived += collect;
            try
            {
                loaded = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
                Assert.That(loaded.IsValid() && loaded.isLoaded, Is.True);
                Assert.That(loaded.GetRootGameObjects().Length, Is.GreaterThan(0));
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
            finally
            {
                Application.logMessageReceived -= collect;
                if (loaded.IsValid() && !existingHandles.Contains(loaded.handle))
                    EditorSceneManager.CloseScene(loaded, true);
                if (priorActive.IsValid() && priorActive.isLoaded) SceneManager.SetActiveScene(priorActive);
            }
        }
    }
}
#endif
