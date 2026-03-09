//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Editor;

public partial class GenStyleEditor : DocumentEditor
{
    private static partial class WidgetIds
    {
        public static partial WidgetId LayerPrompt { get; }
        public static partial WidgetId LayerNegativePrompt { get; }
        public static partial WidgetId LayerStrength { get; }
        public static partial WidgetId LayerGuidance { get; }
        public static partial WidgetId RefinePrompt { get; }
        public static partial WidgetId RefineNegativePrompt { get; }
        public static partial WidgetId RefineStrength { get; }
        public static partial WidgetId RefineGuidance { get; }
        public static partial WidgetId StyleRefStrength { get; }
    }

    public new GenStyleDocument Document => (GenStyleDocument)base.Document;

    public override bool ShowInspector => true;

    public GenStyleEditor(GenStyleDocument doc) : base(doc)
    {
        Commands =
        [
            new Command { Name = "Exit Edit Mode", Handler = Workspace.EndEdit, Key = InputCode.KeyTab },
        ];
    }

    public override void Update()
    {
        Graphics.SetTransform(Document.Transform);
        Document.Draw();
    }

    public override void InspectorUI()
    {
        LayerDefaultsUI();
        RefineDefaultsUI();
        StyleReferencesUI();
    }

    private void LayerDefaultsUI()
    {
        using var _ = Inspector.BeginSection("LAYER DEFAULTS");
        if (Inspector.IsSectionCollapsed) return;

        using (Inspector.BeginRow())
        using (UI.BeginFlex())
            Document.Prompt = UI.TextInput(WidgetIds.LayerPrompt, Document.Prompt, EditorStyle.Inspector.TextArea, "Prompt", Document);

        using (Inspector.BeginRow())
        using (UI.BeginFlex())
            Document.NegativePrompt = UI.TextInput(WidgetIds.LayerNegativePrompt, Document.NegativePrompt, EditorStyle.Inspector.TextArea, "Negative Prompt", Document);

        {
            var strength = Document.DefaultStrength;
            EditorUI.Slider(WidgetIds.LayerStrength, ref strength, 0, 1);
            Document.DefaultStrength = strength;
            UI.HandleChange(Document);
        }

        {
            var guidance = Document.DefaultGuidanceScale;
            EditorUI.Slider(WidgetIds.LayerGuidance, ref guidance, 1.0f, 20.0f);
            Document.DefaultGuidanceScale = guidance;
            UI.HandleChange(Document);
        }
    }

    private void RefineDefaultsUI()
    {
        using var _ = Inspector.BeginSection("REFINE DEFAULTS");
        if (Inspector.IsSectionCollapsed) return;

        using (Inspector.BeginRow())
        using (UI.BeginFlex())
            Document.RefinePrompt = UI.TextInput(WidgetIds.RefinePrompt, Document.RefinePrompt, EditorStyle.Inspector.TextArea, "Refine Prompt", Document);

        using (Inspector.BeginRow())
        using (UI.BeginFlex())
            Document.RefineNegativePrompt = UI.TextInput(WidgetIds.RefineNegativePrompt, Document.RefineNegativePrompt, EditorStyle.Inspector.TextArea, "Negative Prompt", Document);

        {
            var strength = Document.RefineStrength;
            EditorUI.Slider(WidgetIds.RefineStrength, ref strength, 0, 1);
            Document.RefineStrength = strength;
            UI.HandleChange(Document);
        }

        {
            var guidance = Document.RefineGuidanceScale;
            EditorUI.Slider(WidgetIds.RefineGuidance, ref guidance, 1.0f, 20.0f);
            Document.RefineGuidanceScale = guidance;
            UI.HandleChange(Document);
        }
    }

    private void StyleReferencesUI()
    {
        using var _ = Inspector.BeginSection("STYLE REFERENCES");
        if (Inspector.IsSectionCollapsed) return;

        for (int i = 0; i < Document.StyleReferences.Count; i++)
        {
            var (name, strength) = Document.StyleReferences[i];
            using (Inspector.BeginRow())
            {
                UI.Text(name, EditorStyle.Text.Primary);
                EditorUI.Slider(WidgetIds.StyleRefStrength + i, ref strength, 0, 1);
                if (MathF.Abs(strength - Document.StyleReferences[i].Strength) > float.Epsilon)
                    Document.StyleReferences[i] = (name, strength);
                UI.HandleChange(Document);
            }
        }
    }
}
