using System.Globalization;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Development-only frame and memory sampler used for Android evidence.
    /// </summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _gcAllocated;
        private float _nextLogTime;
        private float _lastLogTime;
        private int _lastFrame;

        private void OnEnable()
        {
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _gcAllocated.Dispose();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextLogTime)
            {
                return;
            }

            _nextLogTime = Time.unscaledTime + 5f;
            var elapsed = Time.unscaledTime - _lastLogTime;
            var frames = Mathf.Max(1, Time.frameCount - _lastFrame);
            var averageFrameMs = elapsed * 1000f / frames;
            _lastLogTime = Time.unscaledTime;
            _lastFrame = Time.frameCount;
            Debug.Log(
                $"PERF drawCalls={(_drawCalls.Valid ? _drawCalls.LastValue : -1)} " +
                $"gcAllocated={(_gcAllocated.Valid ? _gcAllocated.LastValue : -1)} " +
                $"monoUsed={Profiler.GetMonoUsedSizeLong()} " +
                $"totalAllocated={Profiler.GetTotalAllocatedMemoryLong()} " +
                $"frameMs={averageFrameMs.ToString("0.00", CultureInfo.InvariantCulture)} " +
                $"frame={Time.frameCount}");
        }
    }
}
