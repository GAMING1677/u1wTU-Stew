using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.UI;
using ApprovalMonster.Core;

namespace ApprovalMonster.Editor
{
    public class SetupTools
    {
        [MenuItem("ApprovalMonster/Setup UI")]
        public static void CreateGameUI()
        {
            // 1. Create Canvas
            var canvasGO = new GameObject("GameCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // 2. Create Background (Safe Area)
            var bgObj = CreateRect("Background", canvasGO.transform);
            Stretch(bgObj.GetComponent<RectTransform>());
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f); // Dark blue-ish

            // 3. Create HUD Container
            var hudObj = CreateRect("HUD", canvasGO.transform);
            var hudRect = hudObj.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(1, 1);
            hudRect.pivot = new Vector2(0.5f, 1);
            hudRect.sizeDelta = new Vector2(0, 100); // Height 100 top bar
            hudRect.anchoredPosition = Vector2.zero;

            // HUD Elements
            var impText = CreateText("ImpressionText", "0 Impressions", hudObj.transform, 36);
            impText.alignment = TextAlignmentOptions.Center;
            impText.rectTransform.anchoredPosition = new Vector2(0, -30);

            var followerText = CreateText("FollowerText", "0 Followers", hudObj.transform, 24);
            followerText.rectTransform.anchorMin = Vector2.zero;
            followerText.rectTransform.anchorMax = Vector2.zero;
            followerText.rectTransform.pivot = Vector2.zero;
            followerText.rectTransform.anchoredPosition = new Vector2(20, 20);

            var mentalText = CreateText("MentalText", "10/10", hudObj.transform, 24);
            mentalText.rectTransform.anchorMin = new Vector2(1, 0);
            mentalText.rectTransform.anchorMax = new Vector2(1, 0);
            mentalText.rectTransform.pivot = new Vector2(1, 0);
            mentalText.rectTransform.anchoredPosition = new Vector2(-20, 20);

            var motivText = CreateText("MotivationText", "AP: 3/3", hudObj.transform, 24);
            motivText.rectTransform.anchoredPosition = new Vector2(0, -70);

            // Mental Slider with visuals
            var sliderObj = CreateRect("MentalSlider", hudObj.transform);
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 20);
            
            // Background
            var bgSlide = CreateRect("Background", sliderObj.transform);
            Stretch(bgSlide.GetComponent<RectTransform>());
            var bgSlideImg = bgSlide.AddComponent<Image>();
            bgSlideImg.color = Color.gray;
            
            // Fill Area
            var fillArea = CreateRect("Fill Area", sliderObj.transform);
            Stretch(fillArea.GetComponent<RectTransform>());
            
            // Fill
            var fill = CreateRect("Fill", fillArea.transform);
            Stretch(fill.GetComponent<RectTransform>());
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.green; // Healthy Mental color
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.maxValue = 10;
            slider.value = 10;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = bgSlideImg;
            slider.direction = Slider.Direction.LeftToRight;

            // 4. Create Hand Container
            var handObj = CreateRect("HandContainer", canvasGO.transform);
            var handRect = handObj.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.5f, 0);
            handRect.anchorMax = new Vector2(0.5f, 0);
            handRect.pivot = new Vector2(0.5f, 0);
            handRect.sizeDelta = new Vector2(800, 300);
            handRect.anchoredPosition = new Vector2(0, 20);

            var layout = handObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            // 5. Setup UIManager
            var uiManagerGO = new GameObject("UIManager");
            var uiManager = uiManagerGO.AddComponent<UIManager>();
            
            // Assign References via SerializedObject to access private fields if needed, 
            // or just use public fields/properties. 
            // Since fields are [SerializeField] private, we use SerializedObject.
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("handContainer").objectReferenceValue = handObj.transform;
            so.FindProperty("followersText").objectReferenceValue = followerText;
            so.FindProperty("mentalText").objectReferenceValue = mentalText;
            so.FindProperty("motivationText").objectReferenceValue = motivText;
            so.FindProperty("impressionText").objectReferenceValue = impText;
            so.FindProperty("mentalSlider").objectReferenceValue = slider;
            
            // We need a Card Prefab. Let's create a template in scene.
            var cardTemplate = CreateCardTemplate(canvasGO.transform);
            cardTemplate.SetActive(false); // Hide it
            so.FindProperty("cardPrefab").objectReferenceValue = cardTemplate.GetComponent<CardView>();
            
            so.ApplyModifiedProperties();

            // 6. Check for EventSystem
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSys = new GameObject("EventSystem");
                eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Debug.Log("UI Setup Complete! created GameCanvas, UIManager, and EventSystem.");
        }

        private static GameObject CreateCardTemplate(Transform parent)
        {
            var cardObj = CreateRect("CardTemplate", parent);
            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 240);
            
            // Background
            var img = cardObj.AddComponent<Image>();
            img.color = Color.white;
            
            // View Component
            var view = cardObj.AddComponent<CardView>();

            // Parts
            var nameTxt = CreateText("Name", "Card Name", cardObj.transform, 18);
            nameTxt.rectTransform.anchoredPosition = new Vector2(0, 90);
            nameTxt.color = Color.black;
            
            var costTxt = CreateText("Cost", "1", cardObj.transform, 24);
            costTxt.rectTransform.anchorMin = new Vector2(0, 1);
            costTxt.rectTransform.anchorMax = new Vector2(0, 1);
            costTxt.rectTransform.pivot = new Vector2(0, 1);
            costTxt.rectTransform.anchoredPosition = new Vector2(10, -10);
            costTxt.color = Color.blue;

            var flavorTxt = CreateText("Flavor", "Description...", cardObj.transform, 12);
            flavorTxt.rectTransform.sizeDelta = new Vector2(140, 80);
            flavorTxt.rectTransform.anchoredPosition = new Vector2(0, -30);
            flavorTxt.color = Color.darkGray;
            
            var mentalTxt = CreateText("MentalCost", "-1 Mental", cardObj.transform, 14);
            mentalTxt.rectTransform.anchorMin = new Vector2(1, 1);
            mentalTxt.rectTransform.anchorMax = new Vector2(1, 1);
            mentalTxt.rectTransform.pivot = new Vector2(1, 1);
            mentalTxt.rectTransform.anchoredPosition = new Vector2(-10, -10);
            mentalTxt.color = Color.red;

            // Icon Placeholder
            var iconObj = CreateRect("Icon", cardObj.transform);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.gray;
            iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 80);
            iconObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            
            // Risk Icon
            var riskObj = CreateRect("RiskIcon", cardObj.transform);
            riskObj.AddComponent<Image>().color = Color.red;
            riskObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            riskObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -100);

            // Assign to View using SerializedObject
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("cardImage").objectReferenceValue = iconImg;
            so.FindProperty("nameText").objectReferenceValue = nameTxt;
            so.FindProperty("costText").objectReferenceValue = costTxt;
            so.FindProperty("flavorText").objectReferenceValue = flavorTxt;
            so.FindProperty("mentalCostText").objectReferenceValue = mentalTxt;
            so.FindProperty("riskIcon").objectReferenceValue = riskObj.GetComponent<Image>();
            so.ApplyModifiedProperties();
            
            // Add CanvasGroup for fade effects
            cardObj.AddComponent<CanvasGroup>();

            return cardObj;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, string content, Transform parent, float fontSize)
        {
            var go = CreateRect(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        [MenuItem("ApprovalMonster/Setup Single Scene UI")]
        public static void SetupSingleSceneUI()
        {
            // 1. Root Canvas
            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                CreateGameUI(); // Call base creator
                canvasGO = GameObject.Find("GameCanvas");
            }
            
            // We need to restructure basic UI into "MainGamePanel"
            // Assume CreateGameUI made HUD and HandContainer directly under Canvas. 
            // We should parent them to MainGamePanel.
            
            var mainPanel = CreateRect("MainGamePanel", canvasGO.transform);
            Stretch(mainPanel.GetComponent<RectTransform>());
            // Find existing HUD and Hand and move them
            var hud = canvasGO.transform.Find("HUD");
            if (hud != null) hud.SetParent(mainPanel.transform, false);
            var hand = canvasGO.transform.Find("HandContainer");
            if (hand != null) hand.SetParent(mainPanel.transform, false);
            // Move UIManager to not be child of canvas usually, but it is separate GO in pure setup.
            
            // 2. Title Panel
            var titlePanel = CreateRect("TitlePanel", canvasGO.transform);
            Stretch(titlePanel.GetComponent<RectTransform>());

            // Move UIManager to MainGamePanel to control lifecycle
            var uiManager = GameObject.FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.transform.SetParent(mainPanel.transform, false);
            }
            titlePanel.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f);
            
            var titleTxt = CreateText("TitleText", "Approval Monster", titlePanel.transform, 64);
            titleTxt.rectTransform.anchoredPosition = new Vector2(0, 50);

            var startBtnObj = CreateRect("StartButton", titlePanel.transform);
            startBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
            startBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            startBtnObj.AddComponent<Image>().color = Color.white;
            var startBtn = startBtnObj.AddComponent<Button>();
            var sBtnTxt = CreateText("Text", "Start Game", startBtnObj.transform, 24);
            sBtnTxt.color = Color.black;

            var tm = titlePanel.AddComponent<TitleManager>();
            var soTM = new SerializedObject(tm);
            soTM.FindProperty("startButton").objectReferenceValue = startBtn;
            soTM.ApplyModifiedProperties();

            // 3. Result Panel
            var resultPanel = CreateRect("ResultPanel", canvasGO.transform);
            Stretch(resultPanel.GetComponent<RectTransform>());
            resultPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Overlay
            
            CreateText("Label", "RESULT", resultPanel.transform, 48).rectTransform.anchoredPosition = new Vector2(0, 100);
            var scoreTxt = CreateText("ScoreText", "0", resultPanel.transform, 72);
            scoreTxt.rectTransform.anchoredPosition = new Vector2(0, 0);

            var backBtnObj = CreateRect("BackButton", resultPanel.transform);
            backBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
            backBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            backBtnObj.AddComponent<Image>().color = Color.white;
            var backBtn = backBtnObj.AddComponent<Button>();
            CreateText("Text", "Back to Title", backBtnObj.transform, 24).color = Color.black;

            var rm = resultPanel.AddComponent<ResultManager>();
            var soRM = new SerializedObject(rm);
            soRM.FindProperty("scoreText").objectReferenceValue = scoreTxt;
            soRM.FindProperty("titleButton").objectReferenceValue = backBtn;
            soRM.ApplyModifiedProperties();
            
            // 4. Setup SceneNavigator
            // Assume attached to GameManager or separate? 
            // In previous setup calls, we didn't attach SceneNavigator automatically in CreateGameUI but user did manually.
            // Let's find it.
            var nav = GameObject.FindObjectOfType<SceneNavigator>();
            if (nav == null)
            {
                var go = new GameObject("SceneNavigator");
                nav = go.AddComponent<SceneNavigator>();
            }
            
            var soNav = new SerializedObject(nav);
            soNav.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            soNav.FindProperty("mainGamePanel").objectReferenceValue = mainPanel;
            soNav.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            // Fade panel?
            var fadeObj = CreateRect("FadePanel", canvasGO.transform);
            Stretch(fadeObj.GetComponent<RectTransform>());
            var fadeImg = fadeObj.AddComponent<Image>();
            fadeImg.color = Color.black;
            fadeImg.raycastTarget = false; // block raycasts handled by logic? usually block when valid.
            fadeObj.SetActive(false);
            soNav.FindProperty("fadePanel").objectReferenceValue = fadeImg;
            
            soNav.ApplyModifiedProperties();

            // Initial State active/inactive
            titlePanel.SetActive(true);
            mainPanel.SetActive(false);
            resultPanel.SetActive(false);
            
            Debug.Log("Single Scene UI Setup Complete!");
        }
    }
}