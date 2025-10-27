using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core.Animation;
using System;

namespace VFXComposer.UI
{
    /// <summary>
    /// 타임라인 뷰 - After Effects 스타일 타임라인 UI
    /// </summary>
    public class TimelineView : VisualElement
    {
        private TimelineController controller;

        // UI Elements
        private VisualElement toolbar;
        private Button playButton;
        private Button pauseButton;
        private Button stopButton;
        private Label timeLabel;

        private VisualElement timeRuler;
        private VisualElement playhead;
        private VisualElement trackList;

        private float pixelsPerSecond = 100f; // 1초당 픽셀 수 (줌 레벨)
        private float scrollOffset = 0f;

        public TimelineView(TimelineController controller)
        {
            this.controller = controller;

            AddToClassList("timeline-view");

            CreateToolbar();
            CreateTimeRuler();
            CreateTrackList();
            CreatePlayhead();

            // 이벤트 구독
            controller.OnTimeChanged += OnTimeChanged;
            controller.OnPlayStateChanged += OnPlayStateChanged;
            controller.OnKeyframeAdded += RefreshTracks;
            controller.OnKeyframeRemoved += RefreshTracks;
        }

        private void CreateToolbar()
        {
            toolbar = new VisualElement();
            toolbar.AddToClassList("timeline-toolbar");
            Add(toolbar);

            // 재생 컨트롤
            playButton = new Button(() => controller.Play());
            playButton.text = "▶";
            playButton.AddToClassList("timeline-button");
            playButton.AddToClassList("timeline-button--play");
            toolbar.Add(playButton);

            pauseButton = new Button(() => controller.Pause());
            pauseButton.text = "⏸";
            pauseButton.AddToClassList("timeline-button");
            pauseButton.AddToClassList("timeline-button--pause");
            toolbar.Add(pauseButton);

            stopButton = new Button(() => controller.Stop());
            stopButton.text = "⏹";
            stopButton.AddToClassList("timeline-button");
            stopButton.AddToClassList("timeline-button--stop");
            toolbar.Add(stopButton);

            // 프레임 이동 버튼
            var prevFrameButton = new Button(() => controller.PreviousFrame());
            prevFrameButton.text = "◀";
            prevFrameButton.AddToClassList("timeline-button");
            prevFrameButton.AddToClassList("timeline-button--prev");
            toolbar.Add(prevFrameButton);

            var nextFrameButton = new Button(() => controller.NextFrame());
            nextFrameButton.text = "▶";
            nextFrameButton.AddToClassList("timeline-button");
            nextFrameButton.AddToClassList("timeline-button--next");
            toolbar.Add(nextFrameButton);

            // 시간 + 프레임 표시
            timeLabel = new Label("0:00.00 [0]");
            timeLabel.AddToClassList("timeline-time-label");
            toolbar.Add(timeLabel);

            // FPS 표시
            var fpsLabel = new Label($"{controller.fps} fps");
            fpsLabel.AddToClassList("timeline-fps-label");
            toolbar.Add(fpsLabel);
        }

        private void CreateTimeRuler()
        {
            timeRuler = new VisualElement();
            timeRuler.AddToClassList("timeline-ruler");
            timeRuler.generateVisualContent += DrawTimeRuler;
            Add(timeRuler);

            // 마우스 클릭으로 시간 이동 (Scrubbing)
            timeRuler.RegisterCallback<MouseDownEvent>(OnTimeRulerClick);
        }

        private void CreateTrackList()
        {
            trackList = new VisualElement();
            trackList.AddToClassList("timeline-tracks");
            trackList.generateVisualContent += DrawTracks;
            Add(trackList);

            // 키프레임 클릭 감지
            trackList.RegisterCallback<MouseDownEvent>(OnTrackListClick);
        }

        private void CreatePlayhead()
        {
            playhead = new VisualElement();
            playhead.AddToClassList("timeline-playhead");
            playhead.pickingMode = PickingMode.Ignore;
            Add(playhead);
        }

        private void DrawTimeRuler(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            painter.lineWidth = 1f;
            painter.strokeColor = new Color(0.5f, 0.5f, 0.5f);

            float width = timeRuler.contentRect.width;
            float height = timeRuler.contentRect.height;

            // 초 단위로 눈금 그리기
            int maxSeconds = Mathf.CeilToInt(controller.duration);
            for (int i = 0; i <= maxSeconds; i++)
            {
                float x = i * pixelsPerSecond + scrollOffset;
                if (x < 0 || x > width) continue;

                // 세로 선
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height - 10));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();

                // 시간 텍스트는 추후 Label로 추가 가능
            }
        }

        private void DrawTracks(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float width = trackList.contentRect.width;
            float trackHeight = 24f;

            // 모든 애니메이션 프로퍼티 가져오기
            var animatedProps = controller.GetAllAnimatedProperties();
            int trackIndex = 0;

            foreach (var kvp in animatedProps)
            {
                string propertyKey = kvp.Key;
                AnimatedProperty animProp = kvp.Value;

                float y = trackIndex * trackHeight;

                // 키프레임 마커 그리기
                foreach (var keyframe in animProp.keyframes)
                {
                    float x = keyframe.time * pixelsPerSecond + scrollOffset;
                    if (x < 0 || x > width) continue;

                    // 다이아몬드 모양 키프레임
                    painter.fillColor = new Color(1f, 0.8f, 0.3f); // 노란색
                    float size = 6f;

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, y + trackHeight / 2 - size));
                    painter.LineTo(new Vector2(x + size, y + trackHeight / 2));
                    painter.LineTo(new Vector2(x, y + trackHeight / 2 + size));
                    painter.LineTo(new Vector2(x - size, y + trackHeight / 2));
                    painter.ClosePath();
                    painter.Fill();
                }

                trackIndex++;
            }
        }

        private void OnTimeRulerClick(MouseDownEvent evt)
        {
            // 클릭 위치를 시간으로 변환
            float x = evt.localMousePosition.x;
            float time = (x - scrollOffset) / pixelsPerSecond;
            time = Mathf.Clamp(time, 0f, controller.duration);

            // 프레임에 스냅
            int frame = controller.TimeToFrame(time);
            controller.SetFrame(frame);

            evt.StopPropagation();
        }

        private void OnTrackListClick(MouseDownEvent evt)
        {
            // TODO: 키프레임 선택/편집
        }

        private void OnTimeChanged(float time)
        {
            // Playhead 위치 업데이트
            float x = time * pixelsPerSecond + scrollOffset;
            playhead.style.left = x;

            // 시간 + 프레임 레이블 업데이트
            int minutes = Mathf.FloorToInt(time / 60f);
            float seconds = time % 60f;
            int frame = controller.CurrentFrame;
            timeLabel.text = $"{minutes}:{seconds:00.00} [{frame}]";

            // 트랙 다시 그리기 (현재 시간 표시)
            trackList.MarkDirtyRepaint();
        }

        private void OnPlayStateChanged(bool playing)
        {
            playButton.SetEnabled(!playing);
            pauseButton.SetEnabled(playing);
        }

        private void RefreshTracks()
        {
            trackList.MarkDirtyRepaint();
        }

        /// <summary>
        /// 줌 레벨 변경
        /// </summary>
        public void SetZoom(float pixelsPerSecond)
        {
            this.pixelsPerSecond = Mathf.Clamp(pixelsPerSecond, 20f, 500f);
            timeRuler.MarkDirtyRepaint();
            trackList.MarkDirtyRepaint();
            OnTimeChanged(controller.currentTime); // Playhead 업데이트
        }
    }
}
