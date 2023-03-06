﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

#if UNITY_EDITOR
#endif

namespace Yarn.Unity.Tests
{

    [TestFixture]
    public class DialogueRunnerTests: IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueRunnerTestSceneGUID = "a04d7174042154a47a29ac4f924e0474";

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueRunnerTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueRunnerTestSceneGUID);
        }

        [UnitySetUp]
        public IEnumerator LoadScene() {
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            ActionManager.ClearAllActions();
            yield return new WaitUntil(() => loaded);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_EndToEnd()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            var testKey = "TemporaryTestingKey";
            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            runner.SaveStateToPlayerPrefs(testKey);
            yield return null;

            bool success = runner.LoadStateFromPlayerPrefs(testKey);
            PlayerPrefs.DeleteKey(testKey);
            Assert.IsTrue(success);

            SaveAndLoad_StorageIntegrity(storage, originals.Item1, originals.Item2, originals.Item3);
        }
        [UnityTest]
        public IEnumerator SaveAndLoad_BadLoad()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            bool success = runner.LoadStateFromPlayerPrefs("invalid key");

            // because the load should have failed this should still be fine
            SaveAndLoad_StorageIntegrity(storage, originals.Item1, originals.Item2, originals.Item3);

            Assert.IsFalse(success);
        }
        [UnityTest]
        public IEnumerator SaveAndLoad_BadSave()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var testKey = "TemporaryTestingKey";
            PlayerPrefs.SetString(testKey,"{}");
            
            var originals = storage.GetAllVariables();

            bool success = runner.LoadStateFromPlayerPrefs(testKey);

            // because the load should have failed this should still be fine
            SaveAndLoad_StorageIntegrity(storage, originals.Item1, originals.Item2, originals.Item3);

            Assert.IsFalse(success);
        }
        private void SaveAndLoad_StorageIntegrity(VariableStorageBehaviour storage, Dictionary<string, float> testFloats, Dictionary<string, string> testStrings, Dictionary<string, bool> testBools)
        {
            var current = storage.GetAllVariables();

            SaveAndLoad_VerifyFloats(current.Item1, testFloats);
            SaveAndLoad_VerifyStrings(current.Item2, testStrings);
            SaveAndLoad_VerifyBools(current.Item3, testBools);
        }
        private void SaveAndLoad_VerifyFloats(Dictionary<string, float> current, Dictionary<string, float> original)
        {
            foreach (var pair in current)
            {
                float originalFloat;
                Assert.IsTrue(original.TryGetValue(pair.Key, out originalFloat),"new key is not inside the original set of variables");
                Assert.AreEqual(originalFloat, pair.Value, "values under the same key are different");
            }
        }
        private void SaveAndLoad_VerifyStrings(Dictionary<string, string> current, Dictionary<string, string> original)
        {
            foreach (var pair in current)
            {
                string originalString;
                Assert.IsTrue(original.TryGetValue(pair.Key, out originalString),"new key is not inside the original set of variables");
                Assert.AreEqual(originalString, pair.Value, "values under the same key are different");
            }
        }
        private void SaveAndLoad_VerifyBools(Dictionary<string, bool> current, Dictionary<string, bool> original)
        {
            foreach (var pair in current)
            {
                bool originalBool;
                Assert.IsTrue(original.TryGetValue(pair.Key, out originalBool),"new key is not inside the original set of variables");
                Assert.AreEqual(originalBool, pair.Value, "values under the same key are different");
            }
        }

        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeHeaders()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are all set inside of TestHeadersAreAccessible.yarn
            // which is part of the test scene project
            var allHeaders = new Dictionary<string, Dictionary<string, List<string>>>();
            var headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"EmptyTags"});
            headers.Add("tags", new List<string>() {string.Empty});
            allHeaders.Add("EmptyTags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"ArbitraryHeaderWithValue"});
            headers.Add("arbitraryheader", new List<string>() {"some-arbitrary-text"});
            allHeaders.Add("ArbitraryHeaderWithValue", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"Tags"});
            headers.Add("tags",new List<string>(){"one two three"});
            allHeaders.Add("Tags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"SingleTagOnly"});
            allHeaders.Add("SingleTagOnly",headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"Comments"});
            headers.Add("tags", new List<string>() {"one two three"});
            allHeaders.Add("Comments", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("contains", new List<string>() {"lots"});
            headers.Add("title", new List<string>() {"LotsOfHeaders"});
            headers.Add("this", new List<string>() {"node"});
            headers.Add("of", new List<string>() {string.Empty});
            headers.Add("blank", new List<string>() {string.Empty});
            headers.Add("others", new List<string>() {"are"});
            headers.Add("headers", new List<string>() {""});
            headers.Add("some", new List<string>() {"are"});
            headers.Add("not", new List<string>() {""});
            allHeaders.Add("LotsOfHeaders", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"DuplicateHeaders"});
            headers.Add("repeat", new List<string>() {"tag1", "tag2", "tag3"});
            allHeaders.Add("DuplicateHeaders", headers);

            foreach (var headerTestData in allHeaders)
            {
                var yarnHeaders = runner.yarnProject.GetHeaders(headerTestData.Key);

                // its possible we got no headers or more/less headers
                // so we need to check we found all the ones we expected to see
                Assert.AreEqual(headerTestData.Value.Count, yarnHeaders.Count);

                foreach (var pair in headerTestData.Value)
                {
                    // is the lust of strings the same as what the yarn program thinks?
                    // ie do we a value that matches each and every one of our tests?
                    CollectionAssert.AreEquivalent(pair.Value, yarnHeaders[pair.Key]);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessInitialValues()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are derived from the declares and sets inside of DialogueRunnerTest.yarn
            var testDefaults = new Dictionary<string, System.IConvertible>();
            testDefaults.Add("$laps", 0);
            testDefaults.Add("$float", 1);
            testDefaults.Add("$string", "this is a string");
            testDefaults.Add("$bool", true);
            testDefaults.Add("$true", false);

            CollectionAssert.AreEquivalent(runner.yarnProject.InitialValues, testDefaults);

            yield return null;
        }
        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeNames()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are derived from the nodes inside of:
            //   - DialogueTest.yarn
            //   - TestHeadersAreAccessible.yarn
            // which are part of the default test scene's project
            var testNodes = new string[]
            {
                "Start",
                "Exit",
                "VariableTest",
                "FunctionTest",
                "FunctionTest2",
                "ExternalFunctionTest",
                "BuiltinsTest",
                "LotsOfVars",
                "EmptyTags",
                "Tags",
                "ArbitraryHeaderWithValue",
                "Comments",
                "SingleTagOnly",
                "LotsOfHeaders",
                "DuplicateHeaders",
            };

            CollectionAssert.AreEquivalent(runner.yarnProject.NodeNames, testNodes);

            yield return null;
        }

        [UnityTest]
        public IEnumerator HandleLine_OnValidYarnFile_SendCorrectLinesToUI()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();

            runner.StartDialogue(runner.startNode);
            yield return null;

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", dialogueUI.CurrentLine);
            dialogueUI.Advance();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.Advance();

            Assert.AreEqual(2, dialogueUI.CurrentOptions.Count);
            Assert.AreEqual("Mir reicht es.", dialogueUI.CurrentOptions[0]);
            Assert.AreEqual("Nochmal!", dialogueUI.CurrentOptions[1]);
        }

        [UnityTest]
        public IEnumerator HandleLine_OnViewsArrayContainingNullElement_SendCorrectLinesToUI()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();

            // Insert a null element into the dialogue views array
            var viewArrayWithNullElement = runner.dialogueViews.ToList();
            viewArrayWithNullElement.Add(null);
            runner.dialogueViews = viewArrayWithNullElement.ToArray();

            runner.StartDialogue(runner.startNode);
            yield return null;

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", dialogueUI.CurrentLine);
            dialogueUI.Advance();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.Advance();

            Assert.AreEqual(2, dialogueUI.CurrentOptions.Count);
            Assert.AreEqual("Mir reicht es.", dialogueUI.CurrentOptions[0]);
            Assert.AreEqual("Nochmal!", dialogueUI.CurrentOptions[1]);
        }


        [TestCase("testCommandInteger DialogueRunner 1 2", "3")]
        [TestCase("testCommandString DialogueRunner a b", "ab")]
        [TestCase("testCommandString DialogueRunner \"a b\" \"c d\"", "a bc d")]
        [TestCase("testCommandGameObject DialogueRunner Sphere", "Sphere")]
        [TestCase("testCommandComponent DialogueRunner Sphere", "Sphere's MeshRenderer")]
        [TestCase("testCommandGameObject DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandComponent DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandNoParameters DialogueRunner", "success")]
        [TestCase("testCommandOptionalParams DialogueRunner 1", "3")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 3", "4")]
        [TestCase("testCommandDefaultName DialogueRunner", "success")]
        [TestCase("testCommandCustomInjector custom", "success")]
        [TestCase("testStaticCommand", "success")]
        [TestCase("testClassWideCustomInjector something", "success")]
        [TestCase("testPrivateStaticCommand", "success")]
        [TestCase("testPrivate something", "success")]
        [TestCase("testCustomParameter Sphere", "Got Sphere")]
        [TestCase("testExternalAssemblyCommand", "success")]
        public void HandleCommand_DispatchesCommands(string test, string expectedLogResult) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Log, expectedLogResult);
            var methodFound = runner.DispatchCommandToGameObject(test, () => {});

            Assert.AreEqual(methodFound, DialogueRunner.CommandDispatchResult.Success);        
        }

        [UnityTest]
        public IEnumerator HandleCommand_DispatchedCommands_StartCoroutines() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            var framesToWait = 5;

            runner.DispatchCommandToGameObject($"testCommandCoroutine DialogueRunner {framesToWait}", () => {});

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0) {
                framesToWait -= 1;
                yield return null;
            }
        }

        [TestCase("testCommandOptionalParams DialogueRunner", "requires between 1 and 2 parameters, but 0 were provided")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 2 3", "requires between 1 and 2 parameters, but 3 were provided")]
        public void HandleCommand_FailsWhenParameterCountNotCorrect(string command, string error) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex(error));
            runner.DispatchCommandToGameObject(command, () => {});
        }

        [TestCase("testCommandInteger DialogueRunner 1 not_an_integer", "Can't convert the given parameter")]
        [TestCase("testCommandCustomInjector asdf", "Non-static method requires a target")]
        public void HandleCommand_FailsWhenParameterTypesNotValid(string command, string error) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex(error));
            runner.DispatchCommandToGameObject(command, () => {});
        }

        [Test]
        public void AddCommandHandler_RegistersCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            runner.AddCommandHandler("test1", () => { Debug.Log("success 1"); } );
            runner.AddCommandHandler("test2", (int val) => { Debug.Log($"success {val}"); } );

            LogAssert.Expect(LogType.Log, "success 1");
            LogAssert.Expect(LogType.Log, "success 2");

            runner.DispatchCommandToRegisteredHandlers("test1", () => {});
            runner.DispatchCommandToRegisteredHandlers("test2 2", () => {});
        }

        [UnityTest]
        public IEnumerator AddCommandHandler_RegistersCoroutineCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

             IEnumerator TestCommandCoroutine(int frameDelay) {
                // Wait the specified number of frames
                while (frameDelay > 0) {
                    frameDelay -= 1;
                    yield return null;
                }
                Debug.Log($"success {Time.frameCount}");
            }

            var framesToWait = 5;

            runner.AddCommandHandler("test", () => runner.StartCoroutine(TestCommandCoroutine(framesToWait)));

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            runner.DispatchCommandToRegisteredHandlers("test", () => {});

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0) {
                framesToWait -= 1;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator VariableStorage_OnExternalChanges_ReturnsExpectedValue() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();
            var variableStorage = GameObject.FindObjectOfType<VariableStorageBehaviour>();

            runner.StartDialogue("VariableTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 0 laps!", dialogueUI.CurrentLine);

            variableStorage.SetValue("$laps", 1);
            runner.Stop();
            runner.StartDialogue("VariableTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 1 laps!", dialogueUI.CurrentLine);

            variableStorage.SetValue("$laps", 5);
            runner.Stop();
            runner.StartDialogue("FunctionTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 25 laps!", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("FunctionTest2");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked arg! i am a pirate no you're not! arg! i am a pirate laps!", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("ExternalFunctionTest");
            yield return null;

            Assert.AreEqual("Jane: Here's a function from code that's in another assembly: 42", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("BuiltinsTest");
            yield return null;

            Assert.AreEqual("Jane: round(3.522) = 4; round_places(3.522, 2) = 3.52; floor(3.522) = 3; floor(-3.522) = -4; ceil(3.522) = 4; ceil(-3.522) = -3; inc(3.522) = 4; inc(4) = 5; dec(3.522) = 3; dec(3) = 2; decimal(3.522) = 0.5220001; int(3.522) = 3; int(-3.522) = -3;", dialogueUI.CurrentLine);

            // dialogueUI.ReadyForNextLine();
        }   

        [TestCase(@"one two three four", new[] {"one", "two", "three", "four"})]
        [TestCase(@"one ""two three"" four", new[] {"one", "two three", "four"})]
        [TestCase(@"one ""two three four", new[] {"one", "two three four"})]
        [TestCase(@"one ""two \""three"" four", new[] {"one", "two \"three", "four"})]
        [TestCase(@"one \two three four", new[] {"one", "\\two", "three", "four"})]
        [TestCase(@"one ""two \\ three"" four", new[] {"one", "two \\ three", "four"})]
        [TestCase(@"one ""two \1 three"" four", new[] {"one", "two \\1 three", "four"})]
        [TestCase(@"one      two", new[] {"one", "two"})]
        public void SplitCommandText_SplitsTextCorrectly(string input, IEnumerable<string> expectedComponents) 
        {
            IEnumerable<string> parsedComponents = DialogueRunner.SplitCommandText(input);

            Assert.AreEqual(expectedComponents, parsedComponents);
        }
    }
}
