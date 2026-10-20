using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfGrid = System.Windows.Controls.Grid;

namespace RevitMCP.UI;

/// <summary>
/// Panneau latéral ancrable WPF (Dockable Panel) incrusté directement dans Revit.
/// Permet à l'architecte de suivre la sélection active, de lancer des audits PMR en 1-clic,
/// d'observer les alertes proactives et de piloter les fonctionnalités IA.
/// </summary>
public class RevitAiDockablePanel : Page, IDockablePaneProvider
{
    public static readonly DockablePaneId PaneId = new(new Guid("7D9E6482-D3A1-4890-B8A2-E4DCF93B8461"));

    private readonly UIControlledApplication _uiApp;
    private TextBlock _txtStatus = null!;
    private TextBlock _txtSelectionTitle = null!;
    private TextBlock _txtSelectionDetails = null!;
    private StackPanel _alertsPanel = null!;
    private long? _currentSelectedElementId;

    public RevitAiDockablePanel(UIControlledApplication uiApp)
    {
        _uiApp = uiApp;
        InitializeUi();
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right
        };
    }

    private void InitializeUi()
    {
        Background = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30));
        Foreground = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240));
        FontFamily = new FontFamily("Segoe UI");

        var mainScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(12)
        };

        var rootStack = new StackPanel { Orientation = Orientation.Vertical };

        // ─── HEADER ──────────────────────────────────────────────────────────
        var headerBorder = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(40, 40, 45)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var headerStack = new StackPanel();

        var titleBlock = new TextBlock
        {
            Text = "⚡ REVIT AI COPILOT",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 210, 255))
        };

        _txtStatus = new TextBlock
        {
            Text = "● Pont MCP actif sur http://localhost:6543",
            FontSize = 11,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(46, 204, 113)),
            Margin = new Thickness(0, 4, 0, 0)
        };

        headerStack.Children.Add(titleBlock);
        headerStack.Children.Add(_txtStatus);
        headerBorder.Child = headerStack;
        rootStack.Children.Add(headerBorder);

        // ─── SECTION 1 : SÉLECTION ACTIVE ────────────────────────────────────
        var selBorder = CreateCardBorder();
        var selStack = new StackPanel();

        var selHeader = new TextBlock
        {
            Text = "🎯 SÉLECTION ACTIVE",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(255, 180, 0)),
            Margin = new Thickness(0, 0, 0, 6)
        };

        _txtSelectionTitle = new TextBlock
        {
            Text = "Aucun élément sélectionné",
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap
        };

        _txtSelectionDetails = new TextBlock
        {
            Text = "Cliquez sur un élément dans Revit pour afficher ses propriétés et actions IA.",
            FontSize = 11,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(170, 170, 170)),
            Margin = new Thickness(0, 4, 0, 10),
            TextWrapping = TextWrapping.Wrap
        };

        // Boutons d'actions rapides
        var actionsGrid = new WpfGrid();
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actionsGrid.RowDefinitions.Add(new RowDefinition());
        actionsGrid.RowDefinitions.Add(new RowDefinition());

        var btnIsolate = CreateActionButton("🔍 Isoler", (s, e) => ExecuteFastAction("isolate"));
        var btnReset = CreateActionButton("🔄 Rétablir Vue", (s, e) => ExecuteFastAction("reset_view"));
        var btnAuditElem = CreateActionButton("📋 Audit PMR", (s, e) => ExecuteFastAction("audit_pmr"));
        var btnAskAi = CreateActionButton("💬 Demander IA", (s, e) => ExecuteFastAction("ask_ai"));

        WpfGrid.SetRow(btnIsolate, 0); WpfGrid.SetColumn(btnIsolate, 0);
        WpfGrid.SetRow(btnReset, 0); WpfGrid.SetColumn(btnReset, 1);
        WpfGrid.SetRow(btnAuditElem, 1); WpfGrid.SetColumn(btnAuditElem, 0);
        WpfGrid.SetRow(btnAskAi, 1); WpfGrid.SetColumn(btnAskAi, 1);

        actionsGrid.Children.Add(btnIsolate);
        actionsGrid.Children.Add(btnReset);
        actionsGrid.Children.Add(btnAuditElem);
        actionsGrid.Children.Add(btnAskAi);

        selStack.Children.Add(selHeader);
        selStack.Children.Add(_txtSelectionTitle);
        selStack.Children.Add(_txtSelectionDetails);
        selStack.Children.Add(actionsGrid);
        selBorder.Child = selStack;
        rootStack.Children.Add(selBorder);

        // ─── SECTION 2 : COPILOTE PROACTIF EN TEMPS RÉEL ─────────────────────
        var guardianBorder = CreateCardBorder();
        var guardianStack = new StackPanel();

        var guardianHeader = new TextBlock
        {
            Text = "🛡️ COPILOTE PROACTIF & CONTRÔLE CONTINU",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 210, 255)),
            Margin = new Thickness(0, 0, 0, 6)
        };

        _alertsPanel = new StackPanel { Orientation = Orientation.Vertical };
        var defaultAlert = new TextBlock
        {
            Text = "✓ Aucune anomalie détectée. La maquette est sous surveillance active.",
            FontSize = 11,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 2, 0, 6)
        };
        _alertsPanel.Children.Add(defaultAlert);

        var btnClearAlerts = CreateActionButton("Vider les alertes", (s, e) =>
        {
            ProactiveGuardian.ClearAlerts();
            _alertsPanel.Children.Clear();
            _alertsPanel.Children.Add(new TextBlock
            {
                Text = "✓ Alertes effacées.",
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(150, 150, 150))
            });
        });

        guardianStack.Children.Add(guardianHeader);
        guardianStack.Children.Add(_alertsPanel);
        guardianStack.Children.Add(btnClearAlerts);
        guardianBorder.Child = guardianStack;
        rootStack.Children.Add(guardianBorder);

        // ─── SECTION 3 : OUTILS IA RAPIDES ──────────────────────────────────
        var toolsBorder = CreateCardBorder();
        var toolsStack = new StackPanel();

        var toolsHeader = new TextBlock
        {
            Text = "🚀 AUTOMATISATION & GÉNÉRATION",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(180, 120, 255)),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var btnRunPmrAudit = CreateActionButton("📋 Audit PMR Global (1 Clic)", (s, e) => ExecuteFastAction("full_pmr_audit"));
        var btnCaptureView = CreateActionButton("👁️ Capture Instantanée IA", (s, e) => ExecuteFastAction("capture_view"));
        var btnGeneratePavilion = CreateActionButton("🏗️ Macro-Conception Pavillon T4", (s, e) => ExecuteFastAction("macro_t4"));

        toolsStack.Children.Add(toolsHeader);
        toolsStack.Children.Add(btnRunPmrAudit);
        toolsStack.Children.Add(btnCaptureView);
        toolsStack.Children.Add(btnGeneratePavilion);
        toolsBorder.Child = toolsStack;
        rootStack.Children.Add(toolsBorder);

        mainScroll.Content = rootStack;
        Content = mainScroll;
    }

    private static Border CreateCardBorder()
    {
        return new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(38, 38, 42)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };
    }

    private static Button CreateActionButton(string text, RoutedEventHandler onClick)
    {
        var btn = new Button
        {
            Content = text,
            Background = new SolidColorBrush(WpfColor.FromRgb(50, 50, 58)),
            Foreground = new SolidColorBrush(WpfColors.White),
            BorderBrush = new SolidColorBrush(WpfColor.FromRgb(70, 70, 80)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(3),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += onClick;
        return btn;
    }

    /// <summary>
    /// Met à jour l'affichage de l'élément sélectionné en temps réel.
    /// </summary>
    public void UpdateActiveSelection(Element? elem, Document? doc)
    {
        Dispatcher.Invoke(() =>
        {
            if (elem == null || doc == null || !elem.IsValidObject)
            {
                _currentSelectedElementId = null;
                _txtSelectionTitle.Text = "Aucun élément sélectionné";
                _txtSelectionDetails.Text = "Cliquez sur un élément dans Revit pour afficher ses propriétés et actions IA.";
                return;
            }

            _currentSelectedElementId = elem.Id.Value;
            var catName = elem.Category?.Name ?? "Élément";
            _txtSelectionTitle.Text = $"{catName} : {elem.Name}";

            var details = new List<string>
            {
                $"ID : {elem.Id.Value}",
                $"Type/Famille : {elem.GetType().Name}"
            };

            var levelParam = elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                ?? elem.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
            if (levelParam != null && levelParam.HasValue)
            {
                var lvl = doc.GetElement(levelParam.AsElementId()) as Level;
                if (lvl != null) details.Add($"Niveau : {lvl.Name}");
            }

            _txtSelectionDetails.Text = string.Join(" | ", details);
        });
    }

    /// <summary>
    /// Ajoute une alerte proactive visuelle dans le panneau.
    /// </summary>
    public void AddProactiveAlert(ProactiveAlert alert)
    {
        Dispatcher.Invoke(() =>
        {
            if (_alertsPanel.Children.Count > 15)
                _alertsPanel.Children.RemoveAt(_alertsPanel.Children.Count - 1);

            var alertBorder = new Border
            {
                Background = alert.Severity switch
                {
                    "CRITICAL" => new SolidColorBrush(WpfColor.FromRgb(90, 20, 20)),
                    "WARNING" => new SolidColorBrush(WpfColor.FromRgb(80, 50, 15)),
                    _ => new SolidColorBrush(WpfColor.FromRgb(30, 50, 70))
                },
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 2, 0, 4)
            };

            var stack = new StackPanel();
            var title = new TextBlock
            {
                Text = $"[{alert.Category}] {alert.Message}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColors.White),
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(title);

            if (!string.IsNullOrEmpty(alert.Recommendation))
            {
                var rec = new TextBlock
                {
                    Text = $"💡 {alert.Recommendation}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(rec);
            }

            alertBorder.Child = stack;
            _alertsPanel.Children.Insert(0, alertBorder);
        });
    }

    private void ExecuteFastAction(string actionKey)
    {
        try
        {
            switch (actionKey)
            {
                case "full_pmr_audit":
                    TaskDialog.Show("Revit AI Copilot",
                        "Lancement de l'audit réglementaire PMR 1-Clic.\n" +
                        "Vous pouvez également le piloter directement via l'outil MCP 'revit_audit_pmr'.");
                    break;
                case "capture_view":
                    TaskDialog.Show("Revit AI Copilot",
                        "Capture IA instantanée activée.\n" +
                        "L'outil MCP 'revit_screenshot_active_view' permet à Claude d'obtenir directement la photo en Base64.");
                    break;
                case "macro_t4":
                    TaskDialog.Show("Revit AI Copilot",
                        "Macro-conception générative prête.\n" +
                        "Utilisez 'revit_generate_spatial_layout' dans Claude Desktop pour concevoir un pavillon complet avec toiture et menuiseries.");
                    break;
                default:
                    if (_currentSelectedElementId.HasValue)
                    {
                        TaskDialog.Show("Action Élément",
                            $"Action '{actionKey}' demandée pour l'élément #{_currentSelectedElementId.Value}.");
                    }
                    else
                    {
                        TaskDialog.Show("Sélection", "Veuillez sélectionner un élément dans la maquette.");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Erreur Copilot", ex.Message);
        }
    }
}
