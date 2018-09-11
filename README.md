# Tween.cs

No need for `using`, everything necessary is in global namespace. Just drop Tween.cs anywhere in your project.

```csharp
yield return new Tween(gameObject)
    .Transform.Position.Add(Vector2.one)
    .Transform.Rotation.Euler.Z.Easing(Easing.Sine).From(0).To(90)
    .Duration(1)
    .Then
    .Color().Easing(Easing.Quint).To(Color.red)
    .Duration(0.5f)
    .Then
    .Alpha().To(0)
    .Duration(1)
    .StartCoroutine(this);
```

`StartCoroutine(this)` is necessary for things to work.
