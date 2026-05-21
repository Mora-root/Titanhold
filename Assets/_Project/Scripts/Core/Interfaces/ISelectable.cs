
public interface ISelectable
{
    bool IsSelectable { get; }

    void OnSelected();
    void OnDeselected();

}
