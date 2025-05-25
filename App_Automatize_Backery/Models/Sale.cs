using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace App_Automatize_Backery.Models;

public partial class Sale
{
    public int SaleId { get; set; }

    private int _userId;
    public int UserId
    {
        get => _userId;
        set
        {
            if (_userId != value)
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }
    }

    private string? _typeSale;
    public string? TypeSale
    {
        get => _typeSale;
        set
        {
            if (_typeSale != value)
            {
                _typeSale = value;
                OnPropertyChanged(nameof(TypeSale));
            }
        }
    }


    private DateTime _dateTimeSale;
    public DateTime DateTimeSale
    {
        get => _dateTimeSale;
        set
        {
            if (_dateTimeSale != value)
            {
                _dateTimeSale = value;
                OnPropertyChanged(nameof(DateTimeSale));
            }
        }
    }

    private decimal? _coastSale;
    public decimal? CoastSale
    {
        get => _coastSale;
        set
        {
            if (_coastSale != value)
            {
                _coastSale = value;
                OnPropertyChanged(nameof(CoastSale));
            }
        }
    }

    private string? _saleStatus;
    public string? SaleStatus
    {
        get => _saleStatus;
        set
        {
            if (_saleStatus != value)
            {
                _saleStatus = value;
                OnPropertyChanged(nameof(SaleStatus));
            }
        }
    }

    public virtual ICollection<Parish> Parishes { get; set; } = new List<Parish>();

    public virtual ICollection<SaleProduct> SaleProducts { get; set; } = new List<SaleProduct>();

    public virtual User User { get; set; } = null!;

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
