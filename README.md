# Tween.cs

No need for `using`, everything necessary is in global namespace. Just drop Tween.cs anywhere in your project.

```csharp
yield return new Tween(gameObject)
    .Position.X.Add(10, Easing.BounceOut)
    .Scale.To(Vector2.one * 0.5f, Easing.BounceOut)
    .Duration(1)
    .Then
    .Color.To(Color.red)
    .Duration(0.5f)
    .StartCoroutine(this);
```

`Tween` class provides shortcuts for `.Transform.Position`, `.Transform.Scale` and `.Transform.Rotation`...

```csharp
yield return new Tween(gameObject)
    // This
    .Position.To(Vector3.left)
    .Scale.X.From(0).Add(2)
    .Rotation.Z.From(0).To(90)
    .Duration(1)
    .Then
    // Equals to this
    .Transform.Position.To(Vector3.left)
    .Transform.Scale.X.From(0).Add(2)
    .Transform.Rotation.Z.From(0).To(90)
    .Duration(1)
    .StartCoroutine(this);
```

... and for color and alpha (works for `Graphic`, `SpriteRenderer` and `CanvasGroup`)

```csharp
yield return new Tween(gameObject)
    .Color.To(Color.red)
    .Alpha.To(0)
    .Duration(1)
    .StartCoroutine(this);
```

There are also a couple of extensions for common components

```csharp
yield return new Tween(gameObject)
    .Image().FillAmount.To(1)
    .Text().FontSize.From(20).To(24)
    .AudioSource().Volume.To(0)
    .Duration(5)
    .StartCoroutine(this);
```

All [easings](https://easings.net) can be passed in `To()` and `Add()` methods

```csharp
yield return new Tween(gameObject)
    .Position.Add(Vector3.one, Easing.SineOut)
    .Scale.X.From(0).Add(2, Easing.BounceInOut)
    .Rotation.Z.From(0).Add(90, Easing.ExpoOutIn)
    .Color.To(Color.red, Easing.QuartIn)
    .Duration(1)
    .StartCoroutine(this);
```

`StartCoroutine(this)` is necessary for things to work.

## Todo

- Different playbacks (like loop, backwards, etc.)
- Attach different game object

## Maybe?
- Custom Unity editor
- Standard tweens (then we will need Tweens to be tweakable after constructing)
