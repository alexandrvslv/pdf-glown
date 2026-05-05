using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PdfGlown.UI.Other
{
    public class TimerAnimation
    {
        private static readonly Dictionary<object, Dictionary<string, TimerAnimation>> cache = new();

        public static void Abort(object component, string name)
        {
            if (cache.TryGetValue(component, out var names)
                && names.TryGetValue(name, out var animation))
            {
                animation.Cancel();
            }
        }

        private Action<double>? action;
        private double currentValue;
        private double newValue;
        private double step;
        private bool cancel;
        private Timer? timer;
        private string name;
        private Dictionary<string, TimerAnimation>? names;
        private Action<double, bool>? finished;
        private List<TimerAnimation>? animations;
        private TimerAnimation? Parent;

        public TimerAnimation()
        {
            name = "Multi";
        }

        public TimerAnimation(Action<double> action, double currentValue, double newValue)
        {
            name = string.Empty;
            this.action = action;
            this.currentValue = currentValue;
            this.newValue = newValue;
        }

        public bool IsCancel => cancel || (Parent?.IsCancel ?? false);

        public bool IsIncrease => newValue > currentValue;

        public void Add(int v1, int v2, TimerAnimation animation)
        {
            animations ??= new List<TimerAnimation>();
            animations.Add(animation);
            animation.Parent = this;
        }

        public void Commit(object component, string name, uint rate, uint length, Action<double, bool> finished)
        {
            if (!cache.TryGetValue(component, out var names))
                cache[component] = names = new Dictionary<string, TimerAnimation>();
            if (names.TryGetValue(name, out var animation))
            {
                if (IsIncrease == animation.IsIncrease
                    && (IsIncrease && newValue > animation.newValue
                        || !IsIncrease && newValue < animation.newValue))
                {
                    animation.newValue = newValue;
                    animation.step = CalculateStep(rate, length);
                }
                return;
            }
            this.name = name;
            this.names = names;
            names[name] = this;
            cancel = false;
            if (action != null)
            {
                Run(rate, length, finished);
            }
            else if (animations != null)
            {
                this.finished = finished;
                foreach (var item in animations)
                {
                    item.cancel = cancel;
                    item.Run(rate, length, (d, b) =>
                    {
                        if (animations.All(x => x.timer == null))
                        {
                            Finish();
                        }
                    });
                }
            }
        }

        private double CalculateStep(uint rate, uint legth)
        {
            var expectedTicks = legth / rate;
            var diff = newValue - currentValue;
            return diff / expectedTicks;
        }

        private void Run(uint rate, uint legth, Action<double, bool> finished)
        {
            this.finished = finished;
            step = CalculateStep(rate, legth);
            if (Math.Abs(step) < 0.001
                || IsCancel)
            {
                action?.Invoke(newValue);
                Finish();
                return;
            }
            timer = new Timer(TimerTick, null, 0, rate);
        }

        public void TimerTick(object? sender)
        {
            currentValue += step;
            action?.Invoke(currentValue);
            if (Math.Abs(currentValue - newValue) < 0.001
                || IsCancel)
            {
                Finish();
            }
        }

        private void Finish()
        {
            timer?.Dispose();
            timer = null;
            names?.Remove(name);
            names = null;
            finished?.Invoke(currentValue, !IsCancel);
        }

        private void Cancel()
        {
            cancel = true;
            names?.Remove(name);
            names = null;
        }


    }
}