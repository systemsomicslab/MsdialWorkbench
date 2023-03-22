using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace CompMs.CommonMVVM.Validator
{
    public class ValidationBehavior : Microsoft.Xaml.Behaviors.Behavior<DependencyObject>
    {
        private int errorCount = 0;

        #region HasView property
        public bool HasViewError
        {
            get { return (bool)GetValue(HasViewErrorProperty); }
            set { SetValue(HasViewErrorProperty, value); }
        }

        public static readonly DependencyProperty HasViewErrorProperty =
            DependencyProperty.Register(
                "HasViewError",
                typeof(bool),
                typeof(ValidationBehavior),
                new UIPropertyMetadata(false)
            );

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            Validation.AddErrorHandler(this.AssociatedObject, this.ErrorHandler);
        }

        protected override void OnDetaching()
        {
            Validation.RemoveErrorHandler(this.AssociatedObject, this.ErrorHandler);
            base.OnDetaching();
        }

        private void ErrorHandler(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                errorCount++;
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                errorCount--;
            }

            this.HasViewError = errorCount != 0;
        }
    }
}

namespace Rfx.Riken.OsakaUniv
{
    public class ValidationBehavior : Behavior<DependencyObject>
    {
        private int errorCount = 0;

        #region HasView property
        public bool HasViewError
        {
            get { return (bool)GetValue(HasViewErrorProperty); }
            set { SetValue(HasViewErrorProperty, value); }
        }

        public static readonly DependencyProperty HasViewErrorProperty =
            DependencyProperty.Register(
                "HasViewError",
                typeof(bool),
                typeof(ValidationBehavior),
                new UIPropertyMetadata(false)
            );

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            Validation.AddErrorHandler(this.AssociatedObject, this.ErrorHandler);
        }

        protected override void OnDetaching()
        {
            Validation.RemoveErrorHandler(this.AssociatedObject, this.ErrorHandler);
            base.OnDetaching();
        }

        private void ErrorHandler(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                errorCount++;
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                errorCount--;
            }

            this.HasViewError = errorCount != 0;
        }
    }
}
