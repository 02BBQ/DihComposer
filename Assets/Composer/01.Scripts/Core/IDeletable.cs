namespace VFXComposer.Core
{
    /// <summary>
    /// 삭제 가능한 객체 인터페이스
    /// </summary>
    public interface IDeletable
    {
        bool CanDelete();
        void Delete();
        string GetDeleteDescription();
    }
}
