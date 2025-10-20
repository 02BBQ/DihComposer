namespace VFXComposer.Core
{
    /// <summary>
    /// 선택 가능한 객체 인터페이스
    /// </summary>
    public interface ISelectable
    {
        bool IsSelected { get; }
        void Select();
        void Deselect();
        void OnSelectionChanged(bool selected);
    }
}
