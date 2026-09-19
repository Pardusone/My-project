using System;
using System.Collections.Generic;

namespace CircuitPuzzle.Circuit
{
    /// <summary>Bounded in-memory undo of construction edits. Reset starts a fresh session history.</summary>
    public sealed class EditorHistory
    {
        private readonly CircuitBoard board;
        private readonly int capacity;
        private readonly List<BoardSnapshot> undo = new List<BoardSnapshot>();

        public bool CanUndo => undo.Count > 0;
        public event Action Changed;

        public EditorHistory(CircuitBoard board, int capacity = 64)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
        }

        /// <summary>Run one edit returning true only when it succeeds. Rejected edits consume no history.</summary>
        public bool Execute(Func<bool> edit)
        {
            if (edit == null)
                throw new ArgumentNullException(nameof(edit));
            BoardSnapshot before = board.Capture();
            int revision = board.Revision;
            if (!edit() || board.Revision == revision)
                return false;
            if (undo.Count == capacity)
                undo.RemoveAt(0);
            undo.Add(before);
            Changed?.Invoke();
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;
            int last = undo.Count - 1;
            board.Restore(undo[last]);
            undo.RemoveAt(last);
            Changed?.Invoke();
            return true;
        }

        public void ResetTo(BoardSnapshot initial)
        {
            board.Reset(initial);
            undo.Clear();
            Changed?.Invoke();
        }
    }
}
