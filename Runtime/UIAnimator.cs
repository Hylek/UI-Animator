using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Created by Daniel Cumbor in 2023

public enum UIAnimatorMode
{
    Fade = 0,
    Scale = 1,
    Move = 2
}

public enum UIAnimatorTrigger
{
    OnEntry,
    OnExit
}

public enum UIAnimatorComponentType
{
    None,
    Image,
    Text,
    Sprite
}

public class UIAnimator : MonoBehaviour, ITransitionEffect
{
    public UIAnimatorData entryAnimation, exitAnimation;
    public Action<bool> TransitionComplete { get; set; }
    public bool manualMode;

    private UIAnimatorComponentType _componentType;
    private bool _canTransition, _isUserInterface, _processed;
    private UIAnimatorTrigger _currentMode;
    private TMP_Text _textComponent;
    private Image _imageComponent;
    private SpriteRenderer _spriteRenderer;
    private RectTransform _rectTransform;
    private float _currentTime;

    private void Awake() => _processed = false;

    private void Update()
    {
        if (!_canTransition) return;

        UpdateAnimation(_currentMode == UIAnimatorTrigger.OnEntry ? entryAnimation : exitAnimation);
    }

    public void StartTransition(UIAnimatorTrigger trigger)
    {
        if (manualMode) return;

        _currentMode = trigger;
        ProcessTransitionRequest(trigger == UIAnimatorTrigger.OnEntry ? entryAnimation : exitAnimation);
    }

    public void StartTransitionManually(UIAnimatorTrigger trigger)
    {
        _currentMode = trigger;
        ProcessTransitionRequest(trigger == UIAnimatorTrigger.OnEntry ? entryAnimation : exitAnimation);
    }

    public bool IsInManualMode() => manualMode;

    private void ProcessTransitionRequest(UIAnimatorData data)
    {
        if (!_processed)
        {
            FindRelevantComponents(data);
        }

        if (data.mode == UIAnimatorMode.Scale && !_processed)
        {
            transform.localScale = new Vector3(data.startValue, data.startValue, data.startValue);
        }

        if (data.startDelay > 0)
        {
            StartCoroutine(BeginDelay(data.startDelay, () => { _canTransition = true; }));
        }
        else
        {
            _canTransition = true;
        }

        _processed = true;
    }

    private void FindRelevantComponents(UIAnimatorData data)
    {
        // Automatically determine component type.
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform == null)
        {
            // Can't be UI.
            _isUserInterface = false;

            if (data.mode == UIAnimatorMode.Move)
            {
                transform.localPosition = data.startPosition;
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _componentType = UIAnimatorComponentType.Sprite;

                if (data.mode != UIAnimatorMode.Fade) return;

                var color = _spriteRenderer.color;
                color = new Color(color.r, color.g,
                    color.b, data.startValue);
                _spriteRenderer.color = color;

                return;
            }
        }
        else
        {
            _isUserInterface = true;

            if (data.mode == UIAnimatorMode.Move)
            {
                _rectTransform.anchoredPosition = data.startPosition;
            }

            _imageComponent = GetComponent<Image>();
            if (_imageComponent != null)
            {
                _componentType = UIAnimatorComponentType.Image;

                if (data.mode != UIAnimatorMode.Fade) return;

                _imageComponent.color = new Color(_imageComponent.color.r, _imageComponent.color.g,
                    _imageComponent.color.b, data.startValue);

                return;
            }

            _textComponent = GetComponent<TMP_Text>();
            if (_textComponent != null)
            {
                _componentType = UIAnimatorComponentType.Text;

                if (data.mode != UIAnimatorMode.Fade) return;

                _textComponent.alpha = data.startValue;

                return;
            }
        }

        _componentType = UIAnimatorComponentType.None;
    }

    private IEnumerator BeginDelay(float delayTime, Action callback)
    {
        yield return new WaitForSeconds(delayTime);

        callback?.Invoke();
    }

    private void UpdateAnimation(UIAnimatorData data)
    {
        _currentTime += Time.deltaTime * data.speedValue;
        switch (data.mode)
        {
            case UIAnimatorMode.Fade:
                ApplyFadeAnimation(data);
                break;

            case UIAnimatorMode.Scale:
                ApplyScaleAnimation(data);
                break;

            case UIAnimatorMode.Move:
                ApplyMoveAnimation(data);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AnimationComplete(UIAnimatorData data)
    {
        _canTransition = false;
        if (data.endDelay > 0)
        {
            StartCoroutine(BeginDelay(data.endDelay,
                () => { TransitionComplete?.Invoke(_currentMode == UIAnimatorTrigger.OnEntry); }));
        }
        else
        {
            TransitionComplete?.Invoke(_currentMode == UIAnimatorTrigger.OnEntry);
        }

        _currentTime = 0f;
    }

    private void ApplyFadeAnimation(UIAnimatorData data)
    {
        var desiredValue = EasingMethods.GetEasingFunction(EasingMethods.Ease.Linear)
            .Invoke(data.startValue, data.endValue, _currentTime);

        switch (_componentType)
        {
            case UIAnimatorComponentType.None:
                Debug.LogError("Fade animation is trying to apply on an invalid gameobject!");
                break;

            case UIAnimatorComponentType.Image:
                _imageComponent.color = new Color(_imageComponent.color.r, _imageComponent.color.g,
                    _imageComponent.color.b, desiredValue);
                break;

            case UIAnimatorComponentType.Text:
                _textComponent.alpha = desiredValue;
                break;

            case UIAnimatorComponentType.Sprite:
                var color = _spriteRenderer.color;
                color = new Color(color.r, color.g,
                    color.b, desiredValue);
                _spriteRenderer.color = color;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_currentTime >= 1)
        {
            AnimationComplete(data);
        }
    }

    private void ApplyScaleAnimation(UIAnimatorData data)
    {
        var desiredValue = EasingMethods.GetEasingFunction(data.easeType)
            .Invoke(data.startValue, data.endValue, _currentTime);
        var desiredVector = new Vector3(desiredValue, desiredValue, desiredValue);
        transform.localScale = desiredVector;

        if (_currentTime >= 1)
        {
            AnimationComplete(data);
        }
    }

    private void ApplyMoveAnimation(UIAnimatorData data)
    {
        var x = EasingMethods.GetEasingFunction(data.easeType)
            .Invoke(data.startPosition.x, data.endPosition.x, _currentTime);
        var y = EasingMethods.GetEasingFunction(data.easeType)
            .Invoke(data.startPosition.y, data.endPosition.y, _currentTime);

        if (_isUserInterface)
        {
            _rectTransform.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            transform.localPosition = new Vector2(x, y);
        }

        if (_currentTime >= 1)
        {
            AnimationComplete(data);
        }
    }
}
