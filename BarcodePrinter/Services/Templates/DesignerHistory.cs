using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
namespace BarcodePrinter.Services.Templates;
public sealed class DesignerHistory
{
    private readonly List<LabelTemplate> undo=[];private readonly Stack<LabelTemplate> redo=new();
    public void Clear() { undo.Clear(); redo.Clear(); }
    public bool CanUndo=>undo.Count>0; public bool CanRedo=>redo.Count>0;
    public void Push(LabelTemplate template) {undo.Add(JsonStore.Clone(template));if(undo.Count>60)undo.RemoveAt(0);redo.Clear();}
    public LabelTemplate Undo(LabelTemplate current) { if(!CanUndo)return current;redo.Push(JsonStore.Clone(current));var previous=undo[^1];undo.RemoveAt(undo.Count-1);return previous; }
    public LabelTemplate Redo(LabelTemplate current) { if(!CanRedo)return current;undo.Add(JsonStore.Clone(current));return redo.Pop(); }
}

