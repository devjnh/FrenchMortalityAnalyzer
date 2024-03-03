using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    public interface IProgress
    {
        void NotifyEnd();
        void NotifyProgress(long position);
        void NotifyStart(long length);

        int Resolution { get; set; }
    }

    public class NullProgress : IProgress
    {
        protected NullProgress() { }
        public static IProgress Instance { get; } = new NullProgress();

        public int Resolution { get; set; }
        public void NotifyEnd() { }
        public void NotifyProgress(long position) { }

        public void NotifyStart(long length) { }
    }

    public abstract class Progress : IProgress
    {
        public int Resolution { get; set; } = 50;

        protected int CurrentProgress { get; private set; } = 0;

        long _TotalLength;
        public void NotifyStart(long length)
        {
            _TotalLength = length;
            CurrentProgress = 0;
            OnStart();
        }
        public void NotifyProgress(long position)
        {
            int newProgress = (int)(position * Resolution / _TotalLength);
            if (CurrentProgress >= newProgress)
                return;
            CurrentProgress = newProgress;
            OnProgress();
        }
        public void NotifyEnd()
        {
            _TotalLength = 0;
            CurrentProgress = 0;
            OnEnd();
        }
        protected abstract void OnProgress();
        protected abstract void OnStart();
        protected abstract void OnEnd();
    }

    public class ConsoleProgress : Progress
    {
        protected ConsoleProgress() { }
        public static IProgress Instance { get; } = new ConsoleProgress();
        protected override void OnStart() => Console.Write("[");
        protected override void OnProgress() => Console.Write("-");
        protected override void OnEnd() => Console.WriteLine("]");
    }
}
