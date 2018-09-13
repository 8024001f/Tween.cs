# Tween.cs

No need for `using`, everything necessary is in global namespace. Just drop Tween.cs anywhere in your project.

```csharp
yield return new Tween(gameObject)
    .Transform.Position.Add(Vector2.one)
    .Transform.Rotation.Z.From(0).To(90, Easing.Sine)
    .Duration(1)
    .Then
    .Color().To(Color.red, Easing.Quint)
    .Duration(0.5f)
    .Then
    .Alpha().To(0)
    .Duration(1)
    .StartCoroutine(this);
```

`StartCoroutine(this)` is necessary for things to work.

## Todo

1. Hardcode common components
2. Proper Color() Alpha() extensions
3. Different playbacks (like loop, backwards, etc.)
4. Attach different game object

## Maybe?
1. Custom Unity editor
2. Standard tweens (then we will need Tweens to be tweakable after constructing)
