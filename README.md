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

## Todo

1. Easings (maybe move easing declaration to tweener terminators)
2. Hardcode common components
3. Proper Color() Alpha() extensions
4. Different playbacks (like loop, backwards, etc.)
5. Attach different game object

## Maybe?
1. Custom Unity editor
2. Standard tweens (then we will need Tweens to be tweakable after constructing)
