# ReactiveTween
Reactive tween.

## Examples
- Create an Animation that will change for 3 seconds and ease in and out using the quadratic curve
```cs
Tween.Create( 0  , 360, 3f, n => Easing.EaseInOut(n, EasingType.Cubic))
.Subscribe(x => arc = x);
```

- Create an Animation that will change for 3 seconds and infinitie loop.
```cs
Observable.Concat(new IObservable<float>[]{
  Tween.Create(0, 360, 3f, n => Easing.EaseInOut(n, EasingType.Linear)),
})
.Repeat()
.Subscribe(x => arc = x);
```

## refers
- https://github.com/fumobox/TweenRx/
- https://github.com/keith-hall/reactive-animation/
