using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using EasingFunction = System.Func<double, float>;
using Debug = XamLib.Diagnostics.Debug;

namespace ReactiveTween
{
    /// <summary>
    /// TweenerのBuilderとして。
    /// </summary>
    public class Tween
    {
        #region Static Members.
        private static readonly Lazy<IObservable<long>> everyFrame = new Lazy<IObservable<long>>(() => Observable.Interval(TimeSpan.FromMilliseconds(1000.0 / FrameRate))   // create a cold observable
                                                                                                                .Publish().RefCount());// only pulse while subscribers are connected);
        #endregion Static Members.

        #region Properties.
        public static int FrameRate { get; set; } = 20;
        internal static IObservable<long> EveryFrame => everyFrame.Value;
        #endregion Properties.

        #region Members.

        #endregion Members.

        /// <summary>
        /// Tweenerの生成
        /// </summary>
        /// <param name="begin">開始位置</param>
        /// <param name="end">終了位置</param>
        /// <param name="duration">補間時間(sec)</param>
        /// <param name="ef">イージング関数</param>
        /// <param name="repeat">繰り返し回数</param>
        /// <param name="delayBefore">イージング開始までの待機時間(sec)</param>
        /// <param name="delayAfter">イージング終了後の待機時間(sec)</param>
        /// <returns></returns>
        public static IObservable<float> Create(float begin, float end, float duration, EasingFunction ef, int repeat = 1, float delayBefore = 0, float delayAfter = 0)
        {
            ef = ef ?? (n => Easing.EaseInOut(n, EasingType.Cubic));

            return new TweenImpl<float>(begin, end, duration, ef, (b, e, n) => (e - b) * n + b, repeat, delayBefore, delayAfter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="begin">開始位置</param>
        /// <param name="end">終了位置</param>
        /// <param name="duration">補間時間(sec)</param>
        /// <param name="ef">イージング関数</param>
        /// <param name="f2t">T型の補間関数</param>
        /// <param name="repeat">繰り返し回数</param>
        /// <param name="delayBefore">イージング開始までの待機時間(sec)</param>
        /// <param name="delayAfter">イージング終了後の待機時間(sec)</param>
        /// <returns></returns>
        public IObservable<T> Create<T>(T begin, T end, float duration, EasingFunction ef, Func<T, T, float, T> f2t, int repeat = 1, float delayBefore = 0, float delayAfter = 0)
        {
            ef = ef ?? (n => Easing.EaseInOut(n, EasingType.Cubic));

            return new TweenImpl<T>(begin, end, duration, ef, f2t, repeat, delayBefore, delayAfter);
        }
    }

    /// <summary>
    /// Tweenerの実装。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TweenImpl<T> : IObservable<T>
    {
        #region Properties.
        /// <summary>
        /// Begin T
        /// </summary>
        public T Begin { get; private set; }

        /// <summary>
        /// End T
        /// </summary>
        public T End { get; private set; }

        /// <summary>
        /// Elapsed Time
        /// </summary>
        public double Elapsed { get; private set; }

        /// <summary>
        /// time of one loop.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// Repeat number.
        /// </summary>
        public int Repeat { get; private set; }
        public float DelayBefore { get; private set; }
        public float DelayAfter { get; private set; }
        public EasingFunction EasingFunction { get; private set; }
        public Func<T, T, float, T> ConvertFunction { get; private set; }

        #endregion Properties.

        #region Members.
        internal IObservable<float> easingStream;
        internal IDisposable execSubscription;
        private  bool isRunning = false;
        #endregion Members.

        /// <summary>
        /// 自分を監視してる人を管理するリスト
        /// </summary>
        private List<IObserver<T>> observers = new List<IObserver<T>>();

        /// <summary>
        /// 同期用object
        /// </summary>
        private object sync = new object();

        /// <summary>
        /// ctor.
        /// 実行にはExecをCallする必要がある
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="duration"></param>
        /// <param name="ef"></param>
        /// <param name="f2t"></param>
        /// <param name="repeat"></param>
        /// <param name="delayBefore"></param>
        /// <param name="delayAfter"></param>
        internal TweenImpl(T begin, T end,
                            float duration,
                            EasingFunction ef,
                            Func<T, T, float, T> f2t,
                            int repeat = 1,
                            float delayBefore = 0, float delayAfter = 0)
        {
            Elapsed         = 0;
            Begin           = begin;
            End             = end;
            Duration        = duration;
            ConvertFunction = f2t;
            EasingFunction  = ef;
            Repeat          = repeat;
            DelayBefore     = delayBefore;
            DelayAfter      = delayAfter;
            easingStream    = Tween.EveryFrame
                .TimeInterval()
                .Where(x => isRunning)
                .Select(x =>
                {
                    lock (sync)
                    {
                        Elapsed += x.Interval.TotalSeconds;
                    }
                    return Elapsed;
                })
                .Select(x => x - DelayBefore)
                .Where(x => x >= 0)
                //.Do(x => Debug.WriteLine($"Elapsed : {x}"))
                .Select(x => {
                    if (x >= Duration + DelayAfter)
                    {
                        return 2.0f;
                    }
                    else
                    {
                        return (float)Math.Min(x / Duration, 1.0);
                    }
                })
                //.Where(x => x <= 1.0)
                .DistinctUntilChanged();        
                //.Select(x => EasingFunction(x));
                //.Select(x => ConvertFunction(Begin, End, x))
        }

        /// <summary>
        /// アニメーション実行
        /// </summary>
        /// <returns></returns>
        public TweenImpl<T> Execute()
        {
            lock (sync)
            {
                Elapsed = 0;
            }

            if (execSubscription != null)
            {
                return this;
            }

            var repeat = Repeat;

            isRunning  = true;

            execSubscription = easingStream
            .Subscribe(x =>
            {
                var n = EasingFunction(Math.Min(x, 1.0));
                var v = ConvertFunction(Begin, End, n);     // easing中のT

                OnNext(v);

                if (x > 1.0)
                {
                    if (--repeat == 0)
                    {
                        OnCompleted();
                    }
                    else
                    {
                        ResetDelayBefore();
                    }
                }
            });

            return this;
        }

        private void ResetDelayBefore()
        {
            lock (sync)
            {
                Elapsed = DelayBefore;
            }
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        public void Pause()
        {
            isRunning = false;
        }

        /// <summary>
        /// 再開
        /// </summary>
        public void Resume()
        {
            isRunning = true;
        }

        /// <summary>
        /// 停止
        /// </summary>
        private void Clear()
        {
            execSubscription?.Dispose();
            execSubscription = null;
        }

        /// <summary>
        /// OnNextを通知
        /// </summary>
        /// <param name="t"></param>
        private void OnNext(T t)
        {
            observers.ForEach(obs => obs.OnNext(t));
        }

        /// <summary>
        /// OnCompletedを通知
        /// </summary>
        private void OnCompleted()
        {
            for (var i = observers.Count; --i >= 0;)
            {
                var obs = observers[i];
                obs.OnCompleted();
            }
        }

        /// <summary>
        /// 購読依頼
        /// </summary>
        /// <param name="observer">通知先</param>
        /// <returns></returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Any())
            {
                Execute();
            }

            observers.Add(observer);
            var result = new RemoveObserverDisposable(this, observer);

            return result;
        }

        /// <summary>
        /// 購読解除依頼
        /// </summary>
        private void OnUnsbscribe(IObserver<T> observer)
        {
            if (observers == null)
            {
                return;
            }

            if (observers.IndexOf(observer) != -1)
            {
                observers.Remove(observer);
            }
            if (!observers.Any())

            {
                // 本流の監視をやめる
                Clear();
            }
        }

        /// <summary>
        /// Disposeが呼ばれたらobserverを監視対象から削除する 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class RemoveObserverDisposable : IDisposable
        {
            TweenImpl<T> parent;
            private IObserver<T> observer;

            public RemoveObserverDisposable(TweenImpl<T> parent, IObserver<T> observer)
            {
                this.parent = parent;
                this.observer  = observer;
            }

            public void Dispose()
            {
                if (this.observer == null)
                {
                    return;
                }
                parent.OnUnsbscribe(observer);
                this.observer  = null;
            }
        }
    }
}
