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
- Create 2 joined Animations.
```cs
Observable.Concat(new IObservable<Vector2>[]{
  Tween.Create(vStart, vEnd1, 3f, n => Easing.EaseInOut(n, EasingType.Linear), (b, e, n) =>{
    var v = new Vector2d();
    v.x = (e.x - b.x) * n + b.x;
    v.y = (e.y - b.y) * n + b.y;
    return v;
  }),
  Tween.Create(vEnd1, vEnd2, 3f, n => Easing.EaseInOut(n, EasingType.Linear), (b, e, n) =>{
    var v = new Vector2d();
    v.x = (e.x - b.x) * n + b.x;
    v.y = (e.y - b.y) * n + b.y;
    return v;
  })  
})
.Repeat()
.Subscribe(x => vPlayer.Position = x);
```

Func<T, T, float, T> f2t

## refers and thanks
- https://github.com/fumobox/TweenRx/
- https://github.com/keith-hall/reactive-animation/
