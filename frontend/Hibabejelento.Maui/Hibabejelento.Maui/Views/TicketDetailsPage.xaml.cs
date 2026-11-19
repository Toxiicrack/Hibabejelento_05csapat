using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class TicketDetailsPage : ContentPage
{
    private int _currentPhase = -1;

    public TicketDetailsPage(TicketDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;

        int targetPhase = width < 720 ? 1 : (width < 1100 ? 2 : 3);
        if (targetPhase == _currentPhase) return;

        _currentPhase = targetPhase;
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

            Grid.SetRow(LeftColumn, 0);
            Grid.SetColumn(LeftColumn, 0);

            Grid.SetRow(RightColumn, 1);
            Grid.SetColumn(RightColumn, 0);
        }
        else if (phase == 2)
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.05, GridUnitType.Star)));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.75, GridUnitType.Star)));

            Grid.SetRow(LeftColumn, 0);
            Grid.SetColumn(LeftColumn, 0);

            Grid.SetRow(RightColumn, 0);
            Grid.SetColumn(RightColumn, 1);
        }
        else
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.1, GridUnitType.Star)));
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2.1, GridUnitType.Star)));

            Grid.SetRow(LeftColumn, 0);
            Grid.SetColumn(LeftColumn, 0);

            Grid.SetRow(RightColumn, 0);
            Grid.SetColumn(RightColumn, 1);
        }
    }
}