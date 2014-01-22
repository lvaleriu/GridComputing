#region

using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PixelLab.Common;

#endregion

namespace GridManagerWpf
{
    /// <summary>
    ///     Interaction logic for GridInfra.xaml
    /// </summary>
    public partial class GridInfra
    {
        private readonly DispatcherTimer m_dispatchTimer = new DispatcherTimer(DispatcherPriority.Background);
        private readonly ICommand _changeCenterCmd;

        public GridInfra()
        {
            InitializeComponent();

            DataContext = this;

            m_dispatchTimer.Interval = TimeSpan.FromSeconds(.1);
            m_dispatchTimer.Tick += churn;

            _changeCenterCmd = new DelegateCommand(ChangeCenter);
        }

        private void churn(object s, EventArgs e)
        {
        }

        public ICommand ChangeCenterCmd
        {
            get { return _changeCenterCmd; }
        }

        private void ChangeCenter(object o)
        {
            theGraph.CenterObject = o;
        }
    }
    /*
    internal class NodeColorConverter : SimpleValueConverter<Node<string>, Brush>
    {
        protected override Brush ConvertBase(Node<string> input)
        {
            long hash = -(long)int.MinValue + input.Item.GetHashCode();
            var index = (int)(hash % App.DemoColors.Count);
            return App.DemoColors[index].ToCachedBrush();
        }
    }
     * */
}