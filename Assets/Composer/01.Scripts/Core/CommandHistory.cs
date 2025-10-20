using System.Collections.Generic;
using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// Command Pattern 인터페이스
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
        string GetDescription();
    }

    /// <summary>
    /// Undo/Redo 히스토리 관리
    /// </summary>
    public class CommandHistory
    {
        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();

        private const int maxHistorySize = 50; // 최대 히스토리 크기

        /// <summary>
        /// 명령 실행 및 히스토리에 추가
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear(); // 새 명령 실행 시 redo 스택 초기화

            // 최대 크기 초과 시 가장 오래된 항목 제거
            if (undoStack.Count > maxHistorySize)
            {
                var temp = new Stack<ICommand>();
                for (int i = 0; i < maxHistorySize; i++)
                {
                    temp.Push(undoStack.Pop());
                }
                undoStack.Clear();
                while (temp.Count > 0)
                {
                    undoStack.Push(temp.Pop());
                }
            }

            Debug.Log($"Command executed: {command.GetDescription()}");
        }

        /// <summary>
        /// Undo 실행
        /// </summary>
        public void Undo()
        {
            if (undoStack.Count == 0)
            {
                Debug.LogWarning("Nothing to undo");
                return;
            }

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);

            Debug.Log($"Undo: {command.GetDescription()}");
        }

        /// <summary>
        /// Redo 실행
        /// </summary>
        public void Redo()
        {
            if (redoStack.Count == 0)
            {
                Debug.LogWarning("Nothing to redo");
                return;
            }

            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);

            Debug.Log($"Redo: {command.GetDescription()}");
        }

        /// <summary>
        /// Undo 가능 여부
        /// </summary>
        public bool CanUndo => undoStack.Count > 0;

        /// <summary>
        /// Redo 가능 여부
        /// </summary>
        public bool CanRedo => redoStack.Count > 0;

        /// <summary>
        /// 히스토리 초기화
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}
