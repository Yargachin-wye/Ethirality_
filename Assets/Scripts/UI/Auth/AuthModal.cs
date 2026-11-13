using System;
using System.Collections;
using System.Text.RegularExpressions;
using Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Auth
{
    // Modal email + one-time-code sign-in, built in code so no scene changes
    // are needed. AuthModal.Show(ok => ...) reports whether the player ended
    // up signed in.
    public sealed class AuthModal : MonoBehaviour
    {
        private const int CodeLength = 6;
        private const int MaxCodeAttempts = 5;
        private const float ResendCooldown = 60f;
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static AuthModal _open;

        private Action<bool> _onClosed;
        private TMP_Text _title, _hint, _status, _primaryLabel, _secondaryLabel;
        private TMP_InputField _input;
        private Button _primary, _secondary, _cancel;
        private bool _codeStep, _busy;
        private string _email;
        private int _failedAttempts;
        private float _resendAt;

        public static void Show(Action<bool> onClosed)
        {
            if (_open != null) return;
            var go = new GameObject("AuthModal");
            _open = go.AddComponent<AuthModal>();
            _open._onClosed = onClosed;
        }

        private void Awake()
        {
            BuildUi();
            ShowEmailStep(null);
        }

        private void OnDestroy()
        {
            if (_open == this) _open = null;
        }

        private void Update()
        {
            if (!_codeStep || _busy) return;
            float left = _resendAt - Time.unscaledTime;
            _secondary.interactable = left <= 0f;
            _secondaryLabel.text = left > 0f ? $"Отправить код ещё раз ({Mathf.CeilToInt(left)})" : "Отправить код ещё раз";
        }

        // ---------- steps

        private void ShowEmailStep(string status)
        {
            _codeStep = false;
            _title.text = "Вход в рейтинг";
            _hint.text = "Укажите почту — мы пришлём одноразовый код. Пароль не нужен, новый аккаунт создаётся автоматически.";
            _input.contentType = TMP_InputField.ContentType.EmailAddress;
            _input.characterLimit = 254;
            _input.text = _email ?? "";
            _input.placeholder.GetComponent<TMP_Text>().text = "email@example.com";
            _primaryLabel.text = "Получить код";
            _secondary.gameObject.SetActive(false);
            SetStatus(status, false);
            SetBusy(false);
            _input.ActivateInputField();
        }

        private void ShowCodeStep()
        {
            _codeStep = true;
            _failedAttempts = 0;
            _title.text = "Введите код";
            _hint.text = $"Код отправлен на {_email}. Проверьте также папку «Спам».";
            _input.contentType = TMP_InputField.ContentType.IntegerNumber;
            _input.characterLimit = CodeLength;
            _input.text = "";
            _input.placeholder.GetComponent<TMP_Text>().text = "123456";
            _primaryLabel.text = "Войти";
            _secondaryLabel.text = "Отправить код ещё раз";
            _secondary.gameObject.SetActive(true);
            SetStatus(null, false);
            SetBusy(false);
            _input.ActivateInputField();
        }

        private void OnPrimary()
        {
            if (_busy) return;
            if (_codeStep) StartCoroutine(SubmitCode());
            else StartCoroutine(SendCode());
        }

        private void OnSecondary()
        {
            if (!_busy && _codeStep && Time.unscaledTime >= _resendAt) StartCoroutine(SendCode());
        }

        private void OnInputChanged(string value)
        {
            if (_codeStep && !_busy && value.Length == CodeLength) StartCoroutine(SubmitCode());
        }

        private IEnumerator SendCode()
        {
            string email = _codeStep ? _email : _input.text.Trim().ToLowerInvariant();
            if (!EmailRegex.IsMatch(email) || email.Length > 254)
            {
                SetStatus("Введите корректный адрес почты", true);
                yield break;
            }
            var client = LeaderboardClient.Instance;
            if (client == null || !client.Configured)
            {
                SetStatus("Рейтинг недоступен", true);
                yield break;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetStatus("Нет сети", true);
                yield break;
            }
            SetBusy(true);
            SetStatus("Отправляем код…", false);
            LeaderboardClient.Result result = null;
            yield return client.StartCoroutine(client.RequestCode(email, r => result = r));
            if (result != null && result.ok)
            {
                _email = email;
                _resendAt = Time.unscaledTime + ResendCooldown;
                ShowCodeStep();
                yield break;
            }
            SetBusy(false);
            SetStatus(RequestError(result), true);
        }

        private IEnumerator SubmitCode()
        {
            string code = _input.text.Trim();
            if (code.Length != CodeLength)
            {
                SetStatus($"Код состоит из {CodeLength} цифр", true);
                yield break;
            }
            var client = LeaderboardClient.Instance;
            if (client == null) yield break;
            SetBusy(true);
            SetStatus("Проверяем код…", false);
            LeaderboardClient.Result result = null;
            yield return client.StartCoroutine(client.VerifyCode(_email, code, r => result = r));
            if (result != null && result.ok && client.IsSignedIn)
            {
                Close(true);
                yield break;
            }
            _input.text = "";
            if (result != null && result.status >= 400 && result.status < 500 && result.status != 429
                && ++_failedAttempts >= MaxCodeAttempts)
            {
                ShowEmailStep("Слишком много неверных попыток. Запросите новый код.");
                yield break;
            }
            SetBusy(false);
            SetStatus(VerifyError(result), true);
            _input.ActivateInputField();
        }

        private static string RequestError(LeaderboardClient.Result result)
        {
            if (result == null || result.status == 0) return "Нет связи с сервером";
            if (result.status == 429) return "Слишком часто. Подождите минуту и попробуйте снова";
            if (result.status == 400 || result.status == 422) return "Не удалось отправить код на этот адрес";
            return "Ошибка сервера, попробуйте позже";
        }

        private static string VerifyError(LeaderboardClient.Result result)
        {
            if (result == null || result.status == 0) return "Нет связи с сервером";
            if (result.status == 429) return "Слишком много попыток. Подождите немного";
            if (result.status >= 500) return "Ошибка сервера, попробуйте позже";
            return "Неверный или просроченный код";
        }

        private void Close(bool ok)
        {
            var callback = _onClosed;
            _onClosed = null;
            Destroy(gameObject);
            callback?.Invoke(ok);
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _input.interactable = !busy;
            _primary.interactable = !busy;
            if (!_codeStep) _secondary.interactable = false;
        }

        private void SetStatus(string text, bool error)
        {
            _status.text = text ?? "";
            _status.color = error ? new Color(1f, 0.45f, 0.45f) : new Color(1f, 1f, 1f, 0.7f);
        }

        // ---------- UI construction

        private void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var dim = NewRect("Dim", transform, typeof(Image));
            Stretch(dim, 0);
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            var box = NewRect("Box", dim, typeof(Image));
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(760, 640);
            box.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

            _title = MakeLabel(box, "Title", 24, 60, 40, FontStyles.Bold);
            _hint = MakeLabel(box, "Hint", 90, 100, 24, FontStyles.Normal);
            _input = MakeInput(box, 210);
            _primary = MakeButton(box, "Primary", 300, 64, new Color(0.2f, 0.5f, 0.9f), out _primaryLabel);
            _secondary = MakeButton(box, "Secondary", 380, 56, new Color(1f, 1f, 1f, 0.15f), out _secondaryLabel);
            _status = MakeLabel(box, "Status", 450, 60, 22, FontStyles.Normal);
            _cancel = MakeButton(box, "Cancel", 540, 56, new Color(1f, 1f, 1f, 0.15f), out var cancelLabel);
            cancelLabel.text = "Отмена";

            _primary.onClick.AddListener(OnPrimary);
            _secondary.onClick.AddListener(OnSecondary);
            _cancel.onClick.AddListener(() => Close(false));
            _input.onValueChanged.AddListener(OnInputChanged);
            _input.onSubmit.AddListener(_ => OnPrimary());
        }

        private static RectTransform NewRect(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (var type in components) go.AddComponent(type);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void TopRow(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(40, 0);
            rect.offsetMax = new Vector2(-40, 0);
            rect.anchoredPosition = new Vector2(0, -top);
            rect.sizeDelta = new Vector2(-80, height);
        }

        private static TMP_Text MakeLabel(RectTransform parent, string name, float top, float height,
            float size, FontStyles style)
        {
            var rect = NewRect(name, parent, typeof(TextMeshProUGUI));
            TopRow(rect, top, height);
            var text = rect.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        private static Button MakeButton(RectTransform parent, string name, float top, float height,
            Color color, out TMP_Text label)
        {
            var rect = NewRect(name, parent, typeof(Image), typeof(Button));
            TopRow(rect, top, height);
            var image = rect.GetComponent<Image>();
            image.color = color;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            var labelRect = NewRect("Label", rect, typeof(TextMeshProUGUI));
            Stretch(labelRect, 0);
            label = labelRect.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 26;
            label.raycastTarget = false;
            return button;
        }

        private static TMP_InputField MakeInput(RectTransform parent, float top)
        {
            var rect = NewRect("Input", parent, typeof(Image));
            TopRow(rect, top, 64);
            rect.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            rect.gameObject.SetActive(false);

            var viewport = NewRect("Viewport", rect, typeof(RectMask2D));
            Stretch(viewport, 14);
            var text = NewRect("Text", viewport, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            Stretch((RectTransform)text.transform, 0);
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholder = NewRect("Placeholder", viewport, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            Stretch((RectTransform)placeholder.transform, 0);
            placeholder.fontSize = 28;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            rect.gameObject.SetActive(true);
            return input;
        }
    }
}
