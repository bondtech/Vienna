﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BondTech.Vienna.Settings
{
    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        public static readonly DependencyProperty RatingValueProperty =
            DependencyProperty.Register("RatingValue", typeof(int), typeof(RatingControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(RatingValueChanged)));


        private int maxValue = 99;

        public int RatingValue
        {
            get { return (int)GetValue(RatingValueProperty); }
            set
            {
                if (value < 0)
                {
                    SetValue(RatingValueProperty, 0);
                }
                else if (value > maxValue)
                {
                    SetValue(RatingValueProperty, maxValue);
                }
                else
                {
                    SetValue(RatingValueProperty, value);
                }
            }
        }

        private static void RatingValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RatingControl parent = sender as RatingControl;
            int ratingValue = (int)e.NewValue;
            UIElementCollection children = ((Grid)(parent.Content)).Children;
            ToggleButton button = null;

            for (int i = 0; i < ratingValue; i++)
            {
                button = children[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = true;
            }

            for (int i = ratingValue; i < children.Count; i++)
            {
                button = children[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = false;
            }
        }

        public RatingControl()
        {
            InitializeComponent();
        }

        private void RatingButtonClickEventHandler(Object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;

            int newRating = int.Parse((String)button.Tag);

            if ((bool)button.IsChecked || newRating < RatingValue)
            {
                RatingValue = newRating;
            }
            else
            {
                RatingValue = newRating - 1;
            }

            e.Handled = true;
        }
    }
}
