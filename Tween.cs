using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Razensoft.Tweens;
using Razensoft.Tweens.ComponentTweeners;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class Tween
{
    private readonly List<TweenerGroup> groups = new List<TweenerGroup> { new TweenerGroup() };
    private readonly GameObject gameObject;

    public Tween(Component component) : this(component.gameObject) { }

    public Tween(GameObject gameObject)
    {
        this.gameObject = gameObject;
    }

    public Tween Duration(float seconds)
    {
        return Duration(TimeSpan.FromSeconds(seconds));
    }

    public Tween Duration(TimeSpan timeSpan)
    {
        CurrentGroup.Duration = timeSpan;
        return this;
    }

    public Coroutine StartCoroutine(MonoBehaviour target)
    {
        return target.StartCoroutine(Coroutine());
    }

    public void AddTweener(Tweener tweener)
    {
        CurrentGroup.Add(tweener);
    }

    public Tween StopWhen(Func<bool> predicate)
    {
        CurrentGroup.StopWhen(predicate);
        return this;
    }

    public Tween Then
    {
        get
        {
            groups.Add(new TweenerGroup());
            return this;
        }
    }

    public Tween ThenDestroy()
    {
        CurrentGroup.ThenDestroy = true;
        return this;
    }

    public ComponentTweeners<T> Component<T>()
    {
        return new ComponentTweeners<T>(this, GameObject.GetComponent<T>());
    }

    public TTweener Component<TComponent, TTweener>(Func<TComponent, TTweener> createTweener)
    {
        var component = gameObject.GetComponent<TComponent>();
        if (component == null)
        {
            throw new InvalidOperationException("Game object doesn't have " + typeof(TComponent).Name + " component.");
        }
        return createTweener(component);
    }

    public TransformTweeners Transform
    {
        get { return new TransformTweeners(this, GameObject.transform); }
    }

    public Vector3Tweener Position
    {
        get { return Transform.Position; }
    }

    public Vector3Tweener Rotation
    {
        get { return Transform.Rotation; }
    }

    public Vector3Tweener Scale
    {
        get { return Transform.Scale; }
    }

    public ColorTweener Color
    {
        get {
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                var graphic = component as Graphic;
                if (graphic != null)
                {
                    return new ComponentTweeners<Graphic>(this, graphic).Color();
                }

                var spriteRenderer = component as SpriteRenderer;
                if (spriteRenderer != null)
                {
                    return new SpriteRendererTweeners(this, spriteRenderer).Color;
                }
            }

            throw new InvalidOperationException("Game object doesn't contain Graphic or SpriteRenderer component.");
        }
    }

    public FloatTweener Alpha
    {
        get {
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                var graphic = component as Graphic;
                if (graphic != null)
                {
                    return new ComponentTweeners<Graphic>(this, graphic).Color().A;
                }

                var spriteRenderer = component as SpriteRenderer;
                if (spriteRenderer != null)
                {
                    return new SpriteRendererTweeners(this, spriteRenderer).Color.A;
                }

                var canvasGroup = component as CanvasGroup;
                if (canvasGroup != null)
                {
                    return new ComponentTweeners<CanvasGroup>(this, canvasGroup)
                        .Float(g => g.alpha, (g, v) => g.alpha = v);
                }
            }

            throw new InvalidOperationException("Game object doesn't contain Graphic, SpriteRenderer or CanvasGroup component.");
        }
    }

    protected TweenerGroup CurrentGroup
    {
        get { return groups.Last(); }
    }

    public GameObject GameObject
    {
        get { return gameObject; }
    }

    private IEnumerator Coroutine()
    {
        var queue = new Queue<TweenerGroup>(groups);
        foreach (var group in queue)
        {
            var elapsed = TimeSpan.Zero;
            var duration = group.Duration;
            while (elapsed < duration && !group.ShouldStop)
            {
                elapsed += TimeSpan.FromSeconds(Time.deltaTime);
                var elapsedFraction = elapsed.TotalSeconds / duration.TotalSeconds;
                group.Tween((float)elapsedFraction);
                yield return new WaitForEndOfFrame();
            }

            if (!group.ShouldStop)
            {
                group.Tween(1);
            }

            if (group.ThenDestroy)
            {
                Object.Destroy(GameObject);
                yield break;
            }
        }
    }
}

[PublicAPI]
public class Easing
{
    private const float Pi = Mathf.PI;
    private const float HalfPi = Pi / 2;
    private const float Ln2 = 0.6931471805599453f;
    private const float Ln210 = 6.931471805599453f;

    private readonly Func<float, float> easing;

    public Easing(Func<float, float> easing)
    {
        this.easing = easing;
    }

    public float Ease(float value)
    {
        if (value <= 0)
        {
            return 0;
        }
        if (value >= 1)
        {
            return 1;
        }
        return easing(value);
    }

    public static Easing Default
    {
        get { return Linear; }
    }

    public static readonly Easing Linear = new Easing(t => t);

    public static readonly Easing SineIn = new Easing(t => -Mathf.Cos(HalfPi * t) + 1);
    public static readonly Easing SineOut = new Easing(t => Mathf.Sin(HalfPi * t));
    public static readonly Easing SineInOut = new Easing(t => -Mathf.Cos(Pi * t) / 2 + 0.5f);
    public static readonly Easing SineOutIn = new Easing(EaseSineOutIn);

    private static float EaseSineOutIn(float t)
    {
        if (t < 0.5f)
        {
            return 0.5f * Mathf.Sin(t * 2 * HalfPi);
        }
        return -0.5f * Mathf.Cos((t * 2 - 1) * HalfPi) + 1;
    }

    public static readonly Easing QuadIn = new Easing(t => t * t);
    public static readonly Easing QuadOut = new Easing(t => -t * (t - 2));
    public static readonly Easing QuadInOut = new Easing(EaseQuadInOut);
    public static readonly Easing QuadOutIn = new Easing(EaseQuadOutIn);

    private static float EaseQuadInOut(float t)
    {
        if (t < 0.5f)
        {
            return t * t * 2;
        }
        return 1 - --t * t * 2;
    }

    private static float EaseQuadOutIn(float t)
    {
        if (t < 0.5f)
        {
            return -0.5f * (t = t * 2) * (t - 2);
        }
        return 0.5f * (t = t * 2 - 1) * t + 0.5f;
    }

    public static readonly Easing CubicIn = new Easing(t => t * t * t);
    public static readonly Easing CubicOut = new Easing(t => 1 + --t * t * t);
    public static readonly Easing CubicInOut = new Easing(EaseCubicInOut);
    public static readonly Easing CubicOutIn = new Easing(t => 0.5f * ((t = t * 2 - 1) * t * t + 1));

    private static float EaseCubicInOut(float t)
    {
        if (t < 0.5f)
        {
            return t * t * t * 4;
        }
        return 1 + --t * t * t * 4;
    }

    public static readonly Easing QuartIn = new Easing(t => t * t * t * t);
    public static readonly Easing QuartOut = new Easing(t => 1 - --t * t * t * t);
    public static readonly Easing QuartInOut = new Easing(EaseQuartInOut);
    public static readonly Easing QuartOutIn = new Easing(EaseQuartOutIn);

    private static float EaseQuartInOut(float t)
    {
        if (t < 0.5f)
        {
            return t * t * t * t * 8;
        }
        return (1 - (t = t * 2 - 2) * t * t * t) / 2 + .5f;
    }

    private static float EaseQuartOutIn(float t)
    {
        if (t < 0.5f)
        {
            return -0.5f * (t = (t = t * 2 - 1) * t) * t + 0.5f;
        }
        return 0.5f * (t = (t = t * 2 - 1) * t) * t + 0.5f;
    }

    public static readonly Easing QuintIn = new Easing(t => t * t * t * t * t);
    public static readonly Easing QuintOut = new Easing(t => 1 + --t * t * t * t * t);
    public static readonly Easing QuintInOut = new Easing(EaseQuintInOut);
    public static readonly Easing QuintOutIn = new Easing(t => 0.5f * ((t = t * 2 - 1) * (t *= t) * t + 1));

    private static float EaseQuintInOut(float t)
    {
        if ((t *= 2) < 1)
        {
            return t * t * t * t * t / 2;
        }
        return ((t -= 2) * t * t * t * t + 2) / 2;
    }

    public static readonly Easing ExpoIn = new Easing(t => Mathf.Exp(Ln210 * (t - 1)));
    public static readonly Easing ExpoOut = new Easing(t => 1 - Mathf.Exp(-Ln210 * t));
    public static readonly Easing ExpoInOut = new Easing(EaseExpoInOut);
    public static readonly Easing ExpoOutIn = new Easing(EaseExpoOutIn);

    private static float EaseExpoInOut(float t)
    {
        if ((t *= 2) < 1)
        {
            return 0.5f * Mathf.Exp(Ln210 * (t - 1));
        }
        return 0.5f * (2 - Mathf.Exp(-Ln210 * (t - 1)));
    }

    private static float EaseExpoOutIn(float t)
    {
        if (t < 0.5f)
        {
            return 0.5f * (1 - Mathf.Exp(-20 * Ln2 * t));
        }
        return 0.5f * (Mathf.Exp(20 * Ln2 * (t - 1)) + 1);
    }

    public static readonly Easing CircIn = new Easing(t => -(Mathf.Sqrt(1 - t * t) - 1));
    public static readonly Easing CircOut = new Easing(t => Mathf.Sqrt(1 - (t - 1) * (t - 1)));
    public static readonly Easing CircInOut = new Easing(EaseCircInOut);
    public static readonly Easing CircOutIn = new Easing(EaseCircOutIn);

    private static float EaseCircInOut(float t)
    {
        if (t < 0.5f)
        {
            return (Mathf.Sqrt(1 - t * t * 4) - 1) / -2;
        }
        return (Mathf.Sqrt(1 - (t * 2 - 2) * (t * 2 - 2)) + 1) / 2;
    }

    private static float EaseCircOutIn(float t)
    {
        if (t < 0.5f)
        {
            return 0.5f * Mathf.Sqrt(1 - (t = t * 2 - 1) * t);
        }
        return -0.5f * (Mathf.Sqrt(1 - (t = t * 2 - 1) * t) - 1 - 1);
    }

    private const float Deviation = 1.70158f;
    private const float Deviation2 = 2.70158f;
    public static readonly Easing BackIn = new Easing(t => t * t * (Deviation2 * t - Deviation));
    public static readonly Easing BackOut = new Easing(t => 1 - --t * t * (-Deviation2 * t - Deviation));
    public static readonly Easing BackInOut = new Easing(EaseBackInOut);
    public static readonly Easing BackOutIn = new Easing(EaseBackOutIn);

    private static float EaseBackInOut(float t)
    {
        t *= 2;
        if (t < 1)
        {
            return 0.5f * t * t * (Deviation2 * t - Deviation);
        }
        t--;
        return 0.5f * (1 - --t * t * (-Deviation2 * t - Deviation)) + 0.5f;
    }

    private static float EaseBackOutIn(float t)
    {
        if (t < 0.5f)
        {
            return 0.5f * ((t = t * 2 - 1) * t * (Deviation2 * t + Deviation) + 1);
        }
        return 0.5f * (t = t * 2 - 1) * t * (Deviation2 * t - Deviation) + 0.5f;
    }

    public static readonly Easing ElasticIn = new Easing(EaseElasticIn);
    public static readonly Easing ElasticOut = new Easing(EaseElasticOut);
    public static readonly Easing ElasticInOut = new Easing(EaseElasticInOut);
    public static readonly Easing ElasticOutIn = new Easing(EaseElasticOutIn);

    private const float Amplitude = 1f;
    private const float Period = 0.0003f;
    private const float QPeriod = Period / 4;

    private static float EaseElasticIn(float t)
    {
        return -(Amplitude * Mathf.Exp(Ln210 * (t -= 1)) * Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period));
    }

    private static float EaseElasticOut(float t)
    {
        return Mathf.Exp(-Ln210 * t) * Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period) + 1;
    }

    private static float EaseElasticInOut(float t)
    {
        if ((t *= 2) < 1)
        {
            return -0.5f * (Amplitude * Mathf.Exp(Ln210 * (t -= 1)) *
                            Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period));
        }
        return Amplitude * Mathf.Exp(-Ln210 * (t -= 1)) * Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period) * 0.5f +
               1;
    }

    private static float EaseElasticOutIn(float t)
    {
        if (t < 0.5f)
        {
            return Amplitude / 2 * Mathf.Exp(-Ln210 * t) * Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period) + 0.5f;
        }
        t = t * 2 - 1;
        return -(Amplitude / 2 * Mathf.Exp(Ln210 * (t -= 1)) * Mathf.Sin((t * 0.001f - QPeriod) * (2 * Pi) / Period)) +
               0.5f;
    }

    public static readonly Easing BounceIn = new Easing(EaseBounceIn);
    public static readonly Easing BounceOut = new Easing(EaseBounceOut);
    public static readonly Easing BounceInOut = new Easing(EaseBounceInOut);
    public static readonly Easing BounceOutIn = new Easing(EaseBounceOutIn);

    private const float B1 = 1 / 2.75f;
    private const float B2 = 2 / 2.75f;
    private const float B3 = 1.5f / 2.75f;
    private const float B4 = 2.5f / 2.75f;
    private const float B5 = 2.25f / 2.75f;
    private const float B6 = 2.625f / 2.75f;

    private const float A1 = 7.5625f;
    private const float A2 = 0.75f;
    private const float A3 = 0.9375f;
    private const float A4 = 0.984375f;

    private static float EaseBounceIn(float t)
    {
        t = 1 - t;
        if (t < B1) return 1 - A1 * t * t;
        if (t < B2) return 1 - (A1 * (t -= B3) * t + A2);
        if (t < B4) return 1 - (A1 * (t -= B5) * t + A3);
        return 1 - (A1 * (t -= B6) * t + A4);
    }

    private static float EaseBounceOut(float t)
    {
        if (t < B1) return A1 * t * t;
        if (t < B2) return A1 * (t -= B3) * t + A2;
        if (t < B4) return A1 * (t -= B5) * t + A3;
        return A1 * (t -= B6) * t + A4;
    }

    private static float EaseBounceInOut(float t)
    {
        if (t < 0.5f)
        {
            t = 1 - t * 2;
            if (t < B1) return (1 - A1 * t * t) / 2;
            if (t < B2) return (1 - (A1 * (t -= B3) * t + A2)) / 2;
            if (t < B4) return (1 - (A1 * (t -= B5) * t + A3)) / 2;
            return (1 - (A1 * (t -= B6) * t + A4)) / 2;
        }
        t = t * 2 - 1;
        if (t < B1) return A1 * t * t / 2 + 0.5f;
        if (t < B2) return (A1 * (t -= B3) * t + A2) / 2 + 0.5f;
        if (t < B4) return (A1 * (t -= B5) * t + A3) / 2 + 0.5f;
        return (A1 * (t -= B6) * t + A4) / 2 + 0.5f;
    }

    private static float EaseBounceOutIn(float t)
    {
        if (t < 0.5f)
        {
            t *= 2;
            if (t < B1) return 0.5f * (A1 * t * t);
            if (t < B2) return 0.5f * (A1 * (t -= B3) * t + A2);
            if (t < B4) return 0.5f * (A1 * (t -= B5) * t + A3);
            return 0.5f * (A1 * (t -= B6) * t + A4);
        }

        t = 1 - (t * 2 - 1);
        if (t < B1) return 0.5f - 0.5f * (A1 * t * t) + 0.5f;
        if (t < B2) return 0.5f - 0.5f * (A1 * (t -= B3) * t + A2) + 0.5f;
        if (t < B4) return 0.5f - 0.5f * (A1 * (t -= B5) * t + A3) + 0.5f;
        return 0.5f - 0.5f * (A1 * (t -= B6) * t + A4) + 0.5f;
    }
}

// ReSharper disable once PartialTypeWithSinglePart
public static partial class TweenExtensions
{
    public static ImageTweeners Image(this Tween tween)
    {
        return tween.Component<Image, ImageTweeners>(c => new ImageTweeners(tween, c));
    }

    public static TextTweeners Text(this Tween tween)
    {
        return tween.Component<Text, TextTweeners>(c => new TextTweeners(tween, c));
    }

    public static SpriteRendererTweeners SpriteRenderer(this Tween tween)
    {
        return tween.Component<SpriteRenderer, SpriteRendererTweeners>(c => new SpriteRendererTweeners(tween, c));
    }

    public static AudioSourceTweeners AudioSource(this Tween tween)
    {
        return tween.Component<AudioSource, AudioSourceTweeners>(c => new AudioSourceTweeners(tween, c));
    }

    public static ColorTweener Color<T>(this ComponentTweeners<T> tweeners) where T: Graphic
    {
        return tweeners.Color(c => c.color, (c, v) => c.color = v);
    }
}

namespace Razensoft.Tweens
{
    public class TweenerGroup
    {
        private readonly List<Tweener> tweeners = new List<Tweener>();
        private Func<bool> stopWhen;

        public TweenerGroup()
        {
            Duration = TimeSpan.Zero;
        }

        public TimeSpan Duration { get; set; }

        public bool ThenDestroy { get; set; }

        public void Add(Tweener tweener)
        {
            tweeners.Add(tweener);
        }

        public void StopWhen(Func<bool> predicate)
        {
            stopWhen = predicate;
        }

        public bool ShouldStop
        {
            get { return stopWhen != null && stopWhen(); }
        }

        public void Tween(float elapsedFraction)
        {
            foreach (var tweener in tweeners)
            {
                tweener.TweenValue(elapsedFraction);
            }
        }
    }

    public abstract class Tweener
    {
        public abstract void TweenValue(float elapsedFraction);
    }

    public abstract class Tweener<T> : Tweener
    {
        private readonly Tween tween;
        private Func<T> getValue;
        private Action<T> setValue;
        private ValueRange<T> range;
        protected Easing Easing = Easing.Default;
        protected TweenerFactory TweenerFactory;

        protected Tweener(Tween tween, Func<T> getValue, Action<T> setValue)
        {
            this.tween = tween;
            this.getValue = getValue;
            this.setValue = setValue;
            TweenerFactory = new TweenerFactory(this.tween);
        }

        public override void TweenValue(float elapsedFraction)
        {
            var easedFraction = Easing.Ease(elapsedFraction);
            Value = LerpUnclamped(range.From, range.To, easedFraction);
        }

        protected abstract T LerpUnclamped(T from, T to, float t);

        protected T Value
        {
            get { return getValue(); }
            set { setValue(value); }
        }

        public ValueRangeBuilder From(T value)
        {
            return new ValueRangeBuilder(value, this);
        }

        public Tween To(T value, Easing easing)
        {
            Easing = easing;
            return To(value);
        }

        public Tween To(T value)
        {
            range = new FromToValueRange<T>(getValue, value);
            AddToTween();
            return tween;
        }

        public Tween Add(T value, Easing easing)
        {
            Easing = easing;
            return Add(value);
        }

        public Tween Add(T value)
        {
            range = new AddValueRange<T>(value, Add, getValue);
            AddToTween();
            return tween;
        }

        protected void AddToTween()
        {
            tween.AddTweener(this);
        }

        protected abstract T Add(T first, T second);

        protected Tween Tween
        {
            get { return tween; }
        }

        public class ValueRangeBuilder
        {
            private readonly T from;
            private readonly Tweener<T> tweener;

            public ValueRangeBuilder(T from, Tweener<T> tweener)
            {
                this.from = from;
                this.tweener = tweener;
            }

            public Tween To(T value, Easing easing)
            {
                tweener.Easing = easing;
                return To(value);
            }

            public Tween To(T value)
            {
                tweener.range = new FromToValueRange<T>(() => from, value);
                tweener.AddToTween();
                return tweener.Tween;
            }

            public Tween Add(T value, Easing easing)
            {
                tweener.Easing = easing;
                return Add(value);
            }

            public Tween Add(T value)
            {
                tweener.range = new AddValueRange<T>(value, tweener.Add, () => from);
                tweener.AddToTween();
                return tweener.Tween;
            }
        }
    }

    public abstract class ValueRange<T>
    {
        protected bool Initialized;
        private T from;
        private readonly Func<T> getInitialValue;

        protected ValueRange(Func<T> getInitialValue)
        {
            this.getInitialValue = getInitialValue;
        }

        public T From
        {
            get
            {
                if (!Initialized)
                {
                    InitializeRange();
                }

                return from;
            }
        }

        public abstract T To { get; }

        protected virtual void InitializeRange()
        {
            from = getInitialValue();
            Initialized = true;
        }
    }

    public class FromToValueRange<T> : ValueRange<T>
    {
        private readonly T to;

        public FromToValueRange(Func<T> getInitialValue, T to) : base(getInitialValue)
        {
            this.to = to;
        }

        public override T To
        {
            get { return to; }
        }
    }

    public class AddValueRange<T> : ValueRange<T>
    {
        private T to;
        private readonly T offset;
        private readonly Func<T, T, T> addValue;

        public AddValueRange(T offset, Func<T, T, T> addValue, Func<T> getInitialValue) : base(getInitialValue)
        {
            this.offset = offset;
            this.addValue = addValue;
        }

        public override T To
        {
            get
            {
                if (!Initialized)
                {
                    InitializeRange();
                }

                return to;
            }
        }

        protected override void InitializeRange()
        {
            base.InitializeRange();
            to = addValue(From, offset);
        }
    }

    public class FloatTweener : Tweener<float>
    {
        public FloatTweener(Tween tween, Func<float> getValue, Action<float> setValue) :
            base(tween, getValue, setValue) { }

        protected override float LerpUnclamped(float from, float to, float t)
        {
            return Mathf.LerpUnclamped(from, to, t);
        }

        protected override float Add(float first, float second)
        {
            return first + second;
        }
    }

    public class Int32Tweener : Tweener<int>
    {
        public Int32Tweener(Tween tween, Func<int> getValue, Action<int> setValue) :
            base(tween, getValue, setValue) { }

        protected override int LerpUnclamped(int from, int to, float t)
        {
            return from + (int) ((to - from) * t);
        }

        protected override int Add(int first, int second)
        {
            return first + second;
        }
    }

    public class Vector3Tweener : Tweener<Vector3>
    {
        public Vector3Tweener(Tween tween, Func<Vector3> getValue, Action<Vector3> setValue) :
            base(tween, getValue, setValue) { }

        public FloatTweener X
        {
            get
            {
                return TweenerFactory.Float(() => Value.x, v =>
                {
                    var color = Value;
                    color.x = v;
                    Value = color;
                });
            }
        }

        public FloatTweener Y
        {
            get
            {
                return TweenerFactory.Float(() => Value.y, v =>
                {
                    var color = Value;
                    color.y = v;
                    Value = color;
                });
            }
        }

        public FloatTweener Z
        {
            get
            {
                return TweenerFactory.Float(() => Value.z, v =>
                {
                    var color = Value;
                    color.z = v;
                    Value = color;
                });
            }
        }

        public ValueRangeBuilder From(Vector2 value)
        {
            Vector3 vector3 = value;
            vector3.z = Value.z;
            return base.From(vector3);
        }

        public Tween To(Vector2 value, Easing easing)
        {
            Easing = easing;
            return To(value);
        }

        public Tween To(Vector2 value)
        {
            Vector3 vector3 = value;
            vector3.z = Value.z;
            return base.To(vector3);
        }

        public Tween Add(Vector2 value, Easing easing)
        {
            Easing = easing;
            return Add(value);
        }

        public Tween Add(Vector2 value)
        {
            Vector3 vector3 = value;
            vector3.z = Value.z;
            return base.Add(vector3);
        }

        protected override Vector3 LerpUnclamped(Vector3 from, Vector3 to, float t)
        {
            return Vector3.LerpUnclamped(from, to, t);
        }

        protected override Vector3 Add(Vector3 first, Vector3 second)
        {
            return first + second;
        }
    }

    public class ColorTweener : Tweener<Color>
    {
        public ColorTweener(Tween tween, Func<Color> getValue, Action<Color> setValue) :
            base(tween, getValue, setValue) { }

        public FloatTweener A
        {
            get
            {
                return TweenerFactory.Float(() => Value.a, v =>
                {
                    var color = Value;
                    color.a = v;
                    Value = color;
                });
            }
        }

        public FloatTweener R
        {
            get
            {
                return TweenerFactory.Float(() => Value.r, v =>
                {
                    var color = Value;
                    color.r = v;
                    Value = color;
                });
            }
        }

        public FloatTweener G
        {
            get
            {
                return TweenerFactory.Float(() => Value.g, v =>
                {
                    var color = Value;
                    color.g = v;
                    Value = color;
                });
            }
        }

        public FloatTweener B
        {
            get
            {
                return TweenerFactory.Float(() => Value.b, v =>
                {
                    var color = Value;
                    color.b = v;
                    Value = color;
                });
            }
        }

        protected override Color LerpUnclamped(Color from, Color to, float t)
        {
            return Color.LerpUnclamped(from, to, t);
        }

        protected override Color Add(Color first, Color second)
        {
            return first + second;
        }
    }

    public class QuaternionTweener : Tweener<Quaternion>
    {
        public QuaternionTweener(Tween tween, Func<Quaternion> getValue, Action<Quaternion> setValue) :
            base(tween, getValue, setValue) { }

        public Vector3Tweener Euler
        {
            get
            {
                return TweenerFactory.Vector3(() => Value.eulerAngles, v =>
                {
                    var quaternion = Value;
                    quaternion.eulerAngles = v;
                    Value = quaternion;
                });
            }
        }

        protected override Quaternion LerpUnclamped(Quaternion from, Quaternion to, float t)
        {
            return Quaternion.LerpUnclamped(from, to, t);
        }

        protected override Quaternion Add(Quaternion first, Quaternion second)
        {
            throw new NotSupportedException();
        }
    }

    public class TweenerFactory
    {
        private readonly Tween tween;

        public TweenerFactory(Tween tween)
        {
            this.tween = tween;
        }

        public Int32Tweener Int32(Func<int> getValue, Action<int> setValue)
        {
            return new Int32Tweener(tween, getValue, setValue);
        }

        public FloatTweener Float(Func<float> getValue, Action<float> setValue)
        {
            return new FloatTweener(tween, getValue, setValue);
        }

        public Vector3Tweener Vector3(Func<Vector3> getValue, Action<Vector3> setValue)
        {
            return new Vector3Tweener(tween, getValue, setValue);
        }

        public QuaternionTweener Quaternion(Func<Quaternion> getValue, Action<Quaternion> setValue)
        {
            return new QuaternionTweener(tween, getValue, setValue);
        }

        public ColorTweener Color(Func<Color> getValue, Action<Color> setValue)
        {
            return new ColorTweener(tween, getValue, setValue);
        }
    }
}

namespace Razensoft.Tweens.ComponentTweeners
{
#pragma warning disable 0108
    public class ComponentTweeners<T>
    {
        private readonly TweenerFactory tweenerFactory;
        private readonly T component;

        public ComponentTweeners(Tween tween, T component)
        {
            this.component = component;
            tweenerFactory = new TweenerFactory(tween);
        }

        protected T Component
        {
            get { return component; }
        }

        public Int32Tweener Int32(Func<T, int> getValue, Action<T, int> setValue)
        {
            return tweenerFactory.Int32(() => getValue(Component), value => setValue(Component, value));
        }

        public FloatTweener Float(Func<T, float> getValue, Action<T, float> setValue)
        {
            return tweenerFactory.Float(() => getValue(Component), value => setValue(Component, value));
        }

        public Vector3Tweener Vector3(Func<T, Vector3> getValue, Action<T, Vector3> setValue)
        {
            return tweenerFactory.Vector3(() => getValue(Component), value => setValue(Component, value));
        }

        public QuaternionTweener Quaternion(Func<T, Quaternion> getValue, Action<T, Quaternion> setValue)
        {
            return tweenerFactory.Quaternion(() => getValue(Component), value => setValue(Component, value));
        }

        public ColorTweener Color(Func<T, Color> getValue, Action<T, Color> setValue)
        {
            return tweenerFactory.Color(() => getValue(Component), value => setValue(Component, value));
        }
    }

    public class TransformTweeners : ComponentTweeners<Transform>
    {
        public TransformTweeners(Tween tween, Transform transform) : base(tween, transform) { }

        public Vector3Tweener Position
        {
            get { return Vector3(c => c.position, (c, v) => c.position = v); }
        }

        public Vector3Tweener Scale
        {
            get { return Vector3(c => c.localScale, (c, v) => c.localScale = v); }
        }

        public Vector3Tweener Rotation
        {
            get { return Quaternion(c => c.rotation, (c, v) => c.rotation = v).Euler; }
        }
    }

    public class ImageTweeners : ComponentTweeners<Image>
    {
        public ImageTweeners(Tween tween, Image component) : base(tween, component) { }

        public FloatTweener FillAmount
        {
            get { return Float(c => c.fillAmount, (c, v) => c.fillAmount = v); }
        }

        public ColorTweener Color
        {
            get { return this.Color(); }
        }
    }

    public class TextTweeners : ComponentTweeners<Text>
    {
        public TextTweeners(Tween tween, Text component) : base(tween, component) { }

        public Int32Tweener FontSize
        {
            get { return Int32(c => c.fontSize, (c, v) => c.fontSize = v); }
        }

        public ColorTweener Color
        {
            get { return this.Color(); }
        }
    }

    public class SpriteRendererTweeners : ComponentTweeners<SpriteRenderer>
    {
        public SpriteRendererTweeners(Tween tween, SpriteRenderer component) : base(tween, component) { }

        public ColorTweener Color
        {
            get { return Color(c => c.color, (c, v) => c.color = v); }
        }
    }

    public class AudioSourceTweeners : ComponentTweeners<AudioSource>
    {
        public AudioSourceTweeners(Tween tween, AudioSource component) : base(tween, component) { }

        public FloatTweener Volume
        {
            get { return Float(c => c.volume, (c, v) => c.volume = v); }
        }
    }
#pragma warning restore 0108
}