using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tweens;
using UnityEngine;
using UnityEngine.UI;

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

    public ComponentTweener<T> Component<T>()
    {
        return new ComponentTweener<T>(this, GameObject.GetComponent<T>());
    }

    public TransformTweener Transform
    {
        get { return new TransformTweener(this, GameObject.transform); }
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
                UnityEngine.Object.Destroy(GameObject);
                yield break;
            }
        }
    }
}

public static partial class TweenExtensions
{
    public static ColorTweener Color(this Tween tween)
    {
        var components = tween.GameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            var graphic = component as Graphic;
            if (graphic != null)
            {
                return new ComponentTweener<Graphic>(tween, graphic).Color(g => g.color,
                    (g, v) => g.color = v);
            }

            var spriteRenderer = component as SpriteRenderer;
            if (spriteRenderer != null)
            {
                return new ComponentTweener<SpriteRenderer>(tween, spriteRenderer).Color(g => g.color,
                    (g, v) => g.color = v);
            }
        }

        throw new NotImplementedException();
    }

    public static FloatTweener Alpha(this Tween tween)
    {
        var components = tween.GameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            var graphic = component as Graphic;
            if (graphic != null)
            {
                return new ComponentTweener<Graphic>(tween, graphic).Float(g => g.color.a,
                    (g, v) =>
                    {
                        var color = g.color;
                        color.a = v;
                        g.color = color;
                    });
            }

            var spriteRenderer = component as SpriteRenderer;
            if (spriteRenderer != null)
            {
                return new ComponentTweener<SpriteRenderer>(tween, spriteRenderer).Float(g => g.color.a,
                    (g, v) =>
                    {
                        var color = g.color;
                        color.a = v;
                        g.color = color;
                    });
            }

            var canvasGroup = component as CanvasGroup;
            if (canvasGroup != null)
            {
                return new ComponentTweener<CanvasGroup>(tween, canvasGroup).Float(g => g.alpha, (g, v) => g.alpha = v);
            }
        }

        throw new NotImplementedException();
    }
}

public class Easing
{
    private readonly Func<float, float> easing;

    public Easing(Func<float, float> easing)
    {
        this.easing = easing;
    }

    public float Ease(float value)
    {
        return easing(value);
    }

    public static Easing Linear
    {
        get { return new Easing(f => f); }
    }

    public static Easing Sine
    {
        get { return new Easing(f => Mathf.Sin(f * Mathf.PI / 2)); }
    }

    public static Easing Quad
    {
        get { return new Easing(f => Mathf.Pow(f, 2f)); }
    }

    public static Easing Cubic
    {
        get { return new Easing(f => Mathf.Pow(f, 3f)); }
    }

    public static Easing Quart
    {
        get { return new Easing(f => Mathf.Pow(f, 4f)); }
    }

    public static Easing Quint
    {
        get { return new Easing(f => Mathf.Pow(f, 5f)); }
    }
}


namespace Tweens
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
        private Easing easing = global::Easing.Linear;
        protected TweenerFactory TweenerFactory;

        protected Tweener(Tween tween)
        {
            this.tween = tween;
            TweenerFactory = new TweenerFactory(this.tween);
        }

        public override void TweenValue(float elapsedFraction)
        {
            var easedFraction = easing.Ease(elapsedFraction);
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

        public Tween To(T value)
        {
            range = new FromToValueRange<T>(getValue, value);
            AddToTween();
            return tween;
        }

        public Tween Add(T value)
        {
            range = new AddValueRange<T>(value, Add, getValue);
            AddToTween();
            return tween;
        }

        public Tweener<T> Bind(Func<T> getValue, Action<T> setValue)
        {
            this.getValue = getValue;
            this.setValue = setValue;
            return this;
        }

        public Tweener<T> Easing(Easing easing)
        {
            this.easing = easing;
            return this;
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

            public Tween To(T value)
            {
                tweener.range = new FromToValueRange<T>(() => from, value);
                tweener.AddToTween();
                return tweener.Tween;
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
        protected bool initialized;
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
                if (!initialized)
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
            initialized = true;
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
                if (!initialized)
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
        public FloatTweener(Tween tween) : base(tween) { }

        protected override float LerpUnclamped(float from, float to, float t)
        {
            return Mathf.LerpUnclamped(from, to, t);
        }

        protected override float Add(float first, float second)
        {
            return first + second;
        }
    }

    public class Vector3Tweener : Tweener<Vector3>
    {
        public Vector3Tweener(Tween tween) : base(tween) { }

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

        public Tween To(Vector2 value)
        {
            Vector3 vector3 = value;
            vector3.z = Value.z;
            return base.To(vector3);
        }

        public Tween Add(Vector2 value)
        {
            Vector3 vector3 = value;
            vector3.z = Value.z;
            return base.Add(vector3);
        }

        // Hiding base to preserve Vector3Tweener typing
        public new Vector3Tweener Easing(Easing easing)
        {
            return (Vector3Tweener)base.Easing(easing);
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
        public ColorTweener(Tween tween) : base(tween) { }

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
        public QuaternionTweener(Tween tween) : base(tween) { }

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

    public class ComponentTweener<T>
    {
        private readonly TweenerFactory tweenerFactory;
        private readonly T component;

        public ComponentTweener(Tween tween, T component)
        {
            this.component = component;
            tweenerFactory = new TweenerFactory(tween);
        }

        protected T Component
        {
            get { return component; }
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

    public class TweenerFactory
    {
        private readonly Tween tween;

        public TweenerFactory(Tween tween)
        {
            this.tween = tween;
        }

        public FloatTweener Float(Func<float> getValue, Action<float> setValue)
        {
            return new FloatTweener(tween).Bind(getValue, setValue) as FloatTweener;
        }

        public Vector3Tweener Vector3(Func<Vector3> getValue, Action<Vector3> setValue)
        {
            return new Vector3Tweener(tween).Bind(getValue, setValue) as Vector3Tweener;
        }

        public QuaternionTweener Quaternion(Func<Quaternion> getValue, Action<Quaternion> setValue)
        {
            return new QuaternionTweener(tween).Bind(getValue, setValue) as QuaternionTweener;
        }

        public ColorTweener Color(Func<Color> getValue, Action<Color> setValue)
        {
            return new ColorTweener(tween).Bind(getValue, setValue) as ColorTweener;
        }
    }

    public class TransformTweener : ComponentTweener<Transform>
    {
        public TransformTweener(Tween tween, Transform transform) : base(tween, transform) { }

        public Vector3Tweener Position
        {
            get { return Vector3(t => t.position, (t, v) => t.position = v); }
        }

        public Vector3Tweener Scale
        {
            get { return Vector3(t => t.localScale, (t, v) => t.localScale = v); }
        }

        public QuaternionTweener Rotation
        {
            get { return Quaternion(t => t.rotation, (t, v) => t.rotation = v); }
        }
    }
}