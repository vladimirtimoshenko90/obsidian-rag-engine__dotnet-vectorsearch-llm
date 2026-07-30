using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ObsidianRagEngine.Ocr.Pipelines.Messenger.Normalization;

/// <summary>
/// Prepares a single messenger panel for Tesseract as a short pipeline:
/// optional 2× upscale (small dark crops) → purple bubble prep → invert (dark) →
/// lift remaining colored bubbles → grayscale/contrast.
/// Does not crop header chrome.
/// </summary>
public sealed class MessengerPanelNormalizer
{
    /// <summary>Mean luminance below this (0…1) → treat panel as dark UI and invert.</summary>
    private const float DarkLuminanceThreshold = 0.45f;

    private const float DarkContrast = 1.2f;
    private const float LightContrast = 1.35f;

    /// <summary>
    /// Upscale only very small dark phone crops. Larger / light panels are already
    /// sharp enough; 2× there tends to fatten strokes and nudge scores down.
    /// </summary>
    private const int UpscaleMinShortSidePx = 350;

    /// <summary>Lanczos upscale factor for qualifying panels.</summary>
    private const float UpscaleFactor = 2f;

    /// <summary>
    /// Returns a normalized PNG of <paramref name="panelBytes"/> suitable for document OCR.
    /// Pipeline: optional upscale → purple prep → invert (dark) → lift bubbles → grayscale/contrast.
    /// </summary>
    public byte[] Normalize(byte[] panelBytes)
    {
        ArgumentNullException.ThrowIfNull(panelBytes);
        if (panelBytes.Length == 0)
            throw new ArgumentException("Panel bytes are empty.", nameof(panelBytes));

        using var image = Image.Load<Rgba32>(panelBytes);
        var isDark = PanelImageOps.AverageLuminance(image) < DarkLuminanceThreshold;

        // Small dark crops only — denser glyphs before purple prep / invert.
        if (isDark)
            PanelImageOps.UpscaleIfNeeded(image, UpscaleMinShortSidePx, UpscaleFactor);

        if (isDark)
            PurpleBubblePrep.PrepDarkTheme(image);
        else
            PurpleBubblePrep.PrepLightTheme(image);

        if (isDark)
            PanelImageOps.Invert(image);

        ColoredBubbleLifter.Lift(image);
        PanelImageOps.ApplyGrayscaleContrast(image, isDark ? DarkContrast : LightContrast);

        return PanelImageOps.EncodePng(image);
    }
}
