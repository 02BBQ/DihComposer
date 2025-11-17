using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core.Animation;
using System;
using System.Collections.Generic;

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
        private VisualElement timeRulerCanvas; // 눈금 그리기용
        private VisualElement timeRulerLabels; // 텍스트 레이블용
        private VisualElement playhead;
        private VisualElement trackList;

        private float pixelsPerSecond = 100f; // 1초당 픽셀 수 (줌 레벨)
        private float scrollOffset = 0f;
        private bool isScrubbing = false; // 타임라인 드래그 중인지

        // Touch gesture variables
        private Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();
        private float lastPinchDistance = 0f;
        private bool isPinching = false;

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

            // FPS 설정 버튼들
            var fpsContainer = new VisualElement();
            fpsContainer.style.flexDirection = FlexDirection.Row;
            fpsContainer.style.alignItems = Align.Center;
            toolbar.Add(fpsContainer);

            var fpsLabel = new Label("FPS:");
            fpsLabel.AddToClassList("timeline-fps-label");
            fpsContainer.Add(fpsLabel);

            // FPS 프리셋 버튼
            var fps24Button = new Button(() => SetFPS(24));
            fps24Button.text = "24";
            fps24Button.AddToClassList("timeline-button");
            fps24Button.AddToClassList("timeline-button--fps");
            fpsContainer.Add(fps24Button);

            var fps30Button = new Button(() => SetFPS(30));
            fps30Button.text = "30";
            fps30Button.AddToClassList("timeline-button");
            fps30Button.AddToClassList("timeline-button--fps");
            fpsContainer.Add(fps30Button);

            var fps60Button = new Button(() => SetFPS(60));
            fps60Button.text = "60";
            fps60Button.AddToClassList("timeline-button");
            fps60Button.AddToClassList("timeline-button--fps");
            fpsContainer.Add(fps60Button);

            var fpsValueLabel = new Label($"{controller.fps}");
            fpsValueLabel.AddToClassList("timeline-fps-value");
            fpsValueLabel.style.minWidth = 30;
            fpsValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            fpsContainer.Add(fpsValueLabel);

            // FPS 값 레이블 업데이트를 위한 참조 저장
            controller.OnTimeChanged += (time) =>
            {
                fpsValueLabel.text = $"{controller.fps}";
            };
        }

        private void CreateTimeRuler()
        {
            timeRuler = new VisualElement();
            timeRuler.AddToClassList("timeline-ruler");
            Add(timeRuler);

            // Canvas for drawing ruler lines
            timeRulerCanvas = new VisualElement();
            timeRulerCanvas.style.position = Position.Absolute;
            timeRulerCanvas.style.width = Length.Percent(100);
            timeRulerCanvas.style.height = Length.Percent(100);
            timeRulerCanvas.generateVisualContent += DrawTimeRuler;
            timeRulerCanvas.pickingMode = PickingMode.Ignore;
            timeRuler.Add(timeRulerCanvas);

            // Container for text labels
            timeRulerLabels = new VisualElement();
            timeRulerLabels.style.position = Position.Absolute;
            timeRulerLabels.style.width = Length.Percent(100);
            timeRulerLabels.style.height = Length.Percent(100);
            timeRulerLabels.pickingMode = PickingMode.Ignore;
            timeRuler.Add(timeRulerLabels);

            // 포인터 이벤트로 스크럽 (드래그로 시간 이동)
            timeRuler.RegisterCallback<PointerDownEvent>(OnTimeRulerPointerDown);
            timeRuler.RegisterCallback<PointerMoveEvent>(OnTimeRulerPointerMove);
            timeRuler.RegisterCallback<PointerUpEvent>(OnTimeRulerPointerUp);
            timeRuler.RegisterCallback<PointerCancelEvent>(OnTimeRulerPointerCancel);

            // 휠 이벤트로 줌
            timeRuler.RegisterCallback<WheelEvent>(OnTimeRulerWheel);

            // 눈금 업데이트 스케줄러
            schedule.Execute(UpdateRulerLabels).Every(100);
        }

        private void CreateTrackList()
        {
            trackList = new VisualElement();
            trackList.AddToClassList("timeline-tracks");
            trackList.generateVisualContent += DrawTracks;
            Add(trackList);

            // 키프레임 클릭 감지 (PointerDownEvent로 변경하여 터치 지원)
            trackList.RegisterCallback<PointerDownEvent>(OnTrackListClick);
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
            float width = timeRulerCanvas.contentRect.width;
            float height = timeRulerCanvas.contentRect.height;

            // 배경
            painter.fillColor = new Color(0.15f, 0.15f, 0.15f);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero);
            painter.LineTo(new Vector2(width, 0));
            painter.LineTo(new Vector2(width, height));
            painter.LineTo(new Vector2(0, height));
            painter.ClosePath();
            painter.Fill();

            float frameDuration = 1f / controller.fps;
            int totalFrames = Mathf.CeilToInt(controller.duration / frameDuration);

            // 줌 레벨에 따라 눈금 간격 조정
            // pixelsPerSecond가 작을수록 (축소) 간격을 넓게
            int tickInterval = pixelsPerSecond > 200f ? 4 : (pixelsPerSecond > 100f ? 16 : 32);
            Debug.Log(tickInterval);
            Debug.Log(pixelsPerSecond);

            // 눈금 표시
            for (int frame = 0; frame <= totalFrames; frame += tickInterval)
            {
                float time = frame * frameDuration;
                float x = time * pixelsPerSecond + scrollOffset;

                if (x < 0 || x > width) continue;

                bool isSecondMark = Mathf.Abs(time - Mathf.Round(time)) < 0.01f; // 초 단위 체크

                // 세로 선 그리기
                painter.lineWidth = isSecondMark ? 2f : 1f;
                painter.strokeColor = isSecondMark ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height - (isSecondMark ? 15 : 8)));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
        }

        private void UpdateRulerLabels()
        {
            if (timeRulerLabels == null) return;

            // 기존 레이블 모두 제거
            timeRulerLabels.Clear();

            float width = timeRuler.contentRect.width;
            float height = timeRuler.contentRect.height;

            float frameDuration = 1f / controller.fps;
            int totalFrames = Mathf.CeilToInt(controller.duration / frameDuration);

            bool showFrameNumbers = pixelsPerSecond > 150f;

            // 줌 레벨에 따라 레이블 간격 조정
            int labelInterval = pixelsPerSecond > 150f ? 2 : (pixelsPerSecond > 75F ? 5 : 10);

            // 레이블 추가
            for (int frame = 0; frame <= totalFrames; frame += labelInterval)
            {
                float time = frame * frameDuration;
                float x = time * pixelsPerSecond + scrollOffset;

                if (x < -50 || x > width + 50) continue; // 화면 밖은 생성 안함

                bool isSecondMark = Mathf.Abs(time - Mathf.Round(time)) < 0.01f;

                // 축소 시에는 초 단위 레이블만 표시
                if (!showFrameNumbers && !isSecondMark) continue;

                var label = new Label(isSecondMark ? $"{Mathf.RoundToInt(time)}s" : $"{frame}");
                label.style.position = Position.Absolute;
                label.style.left = x - 15; // 중앙 정렬을 위해 약간 왼쪽으로
                label.style.top = 2;
                label.style.fontSize = isSecondMark ? 11 : 9;
                label.style.color = isSecondMark ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);
                label.style.unityTextAlign = TextAnchor.UpperCenter;
                label.style.width = 30;

                timeRulerLabels.Add(label);
            }
        }

        private void SetFPS(int newFps)
        {
            controller.fps = newFps;
            timeRulerCanvas.MarkDirtyRepaint();
            UpdateRulerLabels();
            Debug.Log($"[Timeline] FPS set to {newFps}");
        }

        private void DrawTracks(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float width = trackList.contentRect.width;
            float height = trackList.contentRect.height;
            float trackHeight = 24f;

            // ruler의 padding 값을 가져옴 (하드코딩 방지)
            float paddingOffset = timeRuler != null && timeRuler.contentRect.xMin > 0 ? timeRuler.contentRect.x : 16f;

            // 시간 눈금 선 그리기 (ruler와 동일)
            float frameDuration = 1f / controller.fps;
            int totalFrames = Mathf.CeilToInt(controller.duration / frameDuration);
            int tickInterval = pixelsPerSecond > 200f ? 4 : (pixelsPerSecond > 100f ? 16 : 32);

            for (int frame = 0; frame <= totalFrames; frame += tickInterval)
            {
                float time = frame * frameDuration;
                float x = time * pixelsPerSecond + scrollOffset + paddingOffset;

                if (x < 0 || x > width) continue;

                bool isSecondMark = Mathf.Abs(time - Mathf.Round(time)) < 0.01f;

                // 세로 선 그리기
                painter.lineWidth = isSecondMark ? 1.5f : 0.5f;
                painter.strokeColor = isSecondMark ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.25f, 0.25f, 0.25f);

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }

            // 모든 애니메이션 프로퍼티 가져오기
            var animatedProps = controller.GetAllAnimatedProperties();
            int trackIndex = 0;

            foreach (var kvp in animatedProps)
            {
                string propertyKey = kvp.Key;
                AnimatedProperty animProp = kvp.Value;

                float y = trackIndex * trackHeight;

                // 키프레임 마커 그리기 (padding 오프셋 추가)
                foreach (var keyframe in animProp.keyframes)
                {
                    float x = keyframe.time * pixelsPerSecond + scrollOffset + paddingOffset;
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

        private void OnTimeRulerPointerDown(PointerDownEvent evt)
        {
            Debug.Log($"[TimelineView] OnTimeRulerPointerDown - button: {evt.button}, isPrimary: {evt.isPrimary}, pointerType: {evt.pointerType}");

            // Track touches for mobile gestures
            if (evt.pointerType != "mouse")
            {
                activeTouches[evt.pointerId] = evt.position;

                // Check for pinch gesture (2 fingers)
                if (activeTouches.Count == 2)
                {
                    Debug.Log("[TimelineView] Pinch gesture detected");
                    isPinching = true;
                    lastPinchDistance = GetPinchDistance();
                    evt.StopPropagation();
                    return;
                }
            }

            // 터치는 button이 -1일 수 있으므로 isPrimary 체크
            if (evt.button != 0 && evt.button != -1) return;
            if (!evt.isPrimary) return;

            isScrubbing = true;
            // CapturePointer 제거 - 모바일에서 문제 발생

            // 클릭 위치를 시간으로 변환 (contentRect 기준으로 계산)
            // localPosition은 border box 기준이므로 contentRect.x (padding-left)를 빼야 함
            float contentX = evt.localPosition.x - timeRuler.contentRect.x;
            float x = Mathf.Clamp(contentX, 0, timeRuler.contentRect.width);
            UpdateTimeFromPosition(x);

            evt.StopPropagation();
        }

        private void OnTimeRulerPointerMove(PointerMoveEvent evt)
        {
            //Debug.Log($"[TimelineView] OnTimeRulerPointerMove - isScrubbing: {isScrubbing}, isPinching: {isPinching}");

            // Update touch tracking
            if (evt.pointerType != "mouse" && activeTouches.ContainsKey(evt.pointerId))
            {
                activeTouches[evt.pointerId] = evt.position;

                if (isPinching && activeTouches.Count == 2)
                {
                    Debug.Log("[TimelineView] Pinch zooming");
                    // Pinch to zoom
                    float currentDistance = GetPinchDistance();
                    float deltaDistance = currentDistance - lastPinchDistance;

                    if (Mathf.Abs(deltaDistance) > 1f)
                    {
                        float zoomDelta = 1f + (deltaDistance * 0.01f); // 줌 민감도
                        float newPixelsPerSecond = pixelsPerSecond * zoomDelta;
                        SetZoom(newPixelsPerSecond);
                        lastPinchDistance = currentDistance;
                    }
                    evt.StopPropagation();
                    return;
                }
            }

            if (!isScrubbing) return;

            // 드래그 중 시간 업데이트 (contentRect 기준으로 계산)
            // localPosition은 border box 기준이므로 contentRect.x (padding-left)를 빼야 함
            float contentX = evt.localPosition.x - timeRuler.contentRect.x;
            float x = Mathf.Clamp(contentX, 0, timeRuler.contentRect.width);
            UpdateTimeFromPosition(x);

            evt.StopPropagation();
        }

        private void OnTimeRulerPointerUp(PointerUpEvent evt)
        {
            Debug.Log($"[TimelineView] OnTimeRulerPointerUp - isPrimary: {evt.isPrimary}");

            // Update touch tracking
            if (evt.pointerType != "mouse")
            {
                activeTouches.Remove(evt.pointerId);

                if (activeTouches.Count < 2)
                {
                    isPinching = false;
                }
            }

            if (!evt.isPrimary) return;

            if (isScrubbing)
            {
                isScrubbing = false;
                // ReleasePointer 제거 - CapturePointer를 사용하지 않음
            }

            evt.StopPropagation();
        }

        private void OnTimeRulerPointerCancel(PointerCancelEvent evt)
        {
            Debug.Log("[TimelineView] OnTimeRulerPointerCancel");

            if (evt.pointerType != "mouse")
            {
                activeTouches.Remove(evt.pointerId);
                if (activeTouches.Count < 2)
                {
                    isPinching = false;
                }
            }

            isScrubbing = false;
        }

        private void UpdateTimeFromPosition(float x)
        {
            float time = (x - scrollOffset) / pixelsPerSecond;
            time = Mathf.Clamp(time, 0f, controller.duration);

            // 프레임에 스냅
            int frame = controller.TimeToFrame(time);
            controller.SetFrame(frame);
        }

        private void OnTrackListClick(PointerDownEvent evt)
        {
            // TODO: 키프레임 선택/편집
            // 터치 지원을 위해 MouseDownEvent에서 PointerDownEvent로 변경
        }

        private void OnTimeChanged(float time)
        {
            // Playhead 위치 업데이트 (ruler의 padding-left 고려)
            float x = time * pixelsPerSecond + scrollOffset;
            // timeRuler.contentRect.x는 ruler의 padding-left 값
            playhead.style.left = x + (timeRuler.contentRect.xMin > 0 ? timeRuler.contentRect.x : 16);

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

        private void OnTimeRulerWheel(WheelEvent evt)
        {
            Debug.Log($"[TimelineView] OnTimeRulerWheel - delta: {evt.delta.y}");

            float delta = evt.delta.y;
            float zoomDelta = delta > 0 ? 0.9f : 1.1f;

            float newPixelsPerSecond = pixelsPerSecond * zoomDelta;
            SetZoom(newPixelsPerSecond);

            evt.StopPropagation();
        }

        private float GetPinchDistance()
        {
            if (activeTouches.Count < 2) return 0f;

            var touchList = new List<Vector2>(activeTouches.Values);
            return Vector2.Distance(touchList[0], touchList[1]);
        }

        /// <summary>
        /// 줌 레벨 변경
        /// </summary>
        public void SetZoom(float pixelsPerSecond)
        {
            this.pixelsPerSecond = Mathf.Clamp(pixelsPerSecond, 20f, 500f);
            timeRulerCanvas.MarkDirtyRepaint();
            trackList.MarkDirtyRepaint();
            UpdateRulerLabels(); // 레이블 즉시 업데이트
            OnTimeChanged(controller.currentTime); // Playhead 업데이트
        }
    }
}
