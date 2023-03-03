using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace edu.ucdavis.fiehnlab.MonaExport.Behaviors {
	public class CloseWindowBehavior : Behavior<Window> {
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(ICommand), typeof(CloseWindowBehavior));

		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.Register("CommandParameter", typeof(object), typeof(CloseWindowBehavior));


		public static readonly DependencyProperty CloseButtonProperty =
			DependencyProperty.Register("CloseButton", typeof(Button), typeof(CloseWindowBehavior));

		public ICommand Command {
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}
		public object CommandParameter {
			get { return GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}
		public Button CloseButton {
			get { return (Button)GetValue(CloseButtonProperty); }
			set { SetValue(CloseButtonProperty, value); }
		}

		private static void OnButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var window = ((CloseWindowBehavior)d).AssociatedObject;
			((Button)e.NewValue).Click += (s, e1) => {
				var command = ((CloseWindowBehavior)d).Command;
				var commandParameter = ((CloseWindowBehavior)d).CommandParameter;

				if(command!=null) {
					command.Execute(commandParameter);
				}

				window.Close();
			};
		}
	}
}
