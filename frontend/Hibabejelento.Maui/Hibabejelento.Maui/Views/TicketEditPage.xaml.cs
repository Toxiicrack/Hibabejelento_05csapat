using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class TicketEditPage : ContentPage
{
    private int _currentResponsivePhase = -1;

    public TicketEditPage(TicketEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;

        int targetPhase = width < 720 ? 1 : (width < 1100 ? 2 : 3);
        if (targetPhase == _currentResponsivePhase) return;

        _currentResponsivePhase = targetPhase;
        ApplyResponsiveLayout(targetPhase);
    }

    private void ApplyResponsiveLayout(int phase)
    {
        ContentGrid.RowDefinitions.Clear();
        ContentGrid.ColumnDefinitions.Clear();

        if (phase == 1)
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            Grid.SetRow(LeftRailGrid, 0);
            Grid.SetColumn(LeftRailGrid, 0);

            Grid.SetRow(RightRailGrid, 1);
            Grid.SetColumn(RightRailGrid, 0);
        }
        else if (phase == 2)
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.35, GridUnitType.Star)));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            Grid.SetRow(LeftRailGrid, 0);
            Grid.SetColumn(LeftRailGrid, 0);

            Grid.SetRow(RightRailGrid, 0);
            Grid.SetColumn(RightRailGrid, 1);
        }
        else
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.5, GridUnitType.Star)));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            Grid.SetRow(LeftRailGrid, 0);
            Grid.SetColumn(LeftRailGrid, 0);

            Grid.SetRow(RightRailGrid, 0);
            Grid.SetColumn(RightRailGrid, 1);
        }
    }
}