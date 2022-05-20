using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin.Control {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class TreeView : ContentView {

      #region Events

      public class TreeViewNodeEventArgs : EventArgs {
         /// <summary>
         /// auslösender <see cref="TreeViewNode"/> oder null, wenn vom <see cref="TreeView"/>
         /// </summary>
         public TreeViewNode TreeViewNode { get; }

         public TreeViewNodeEventArgs(TreeViewNode tn) {
            TreeViewNode = tn;
         }
      }

      public class TreeViewNodeStatusChangedEventArgs : TreeViewNodeEventArgs {

         public bool Cancel { get; set; }

         public TreeViewNodeStatusChangedEventArgs(TreeViewNode tn) : base(tn) {
            Cancel = false;
         }

      }

      public class TreeViewNodeChangedEventArgs : TreeViewNodeEventArgs {

         public TreeViewNode OldTreeViewNode { get; }

         public TreeViewNodeChangedEventArgs(TreeViewNode tn, TreeViewNode oldtn) : base(tn) {
            OldTreeViewNode = oldtn;
         }
      }


      /// <summary>
      /// Bevor sich bei einem <see cref="TreeViewNode"/> <see cref="TreeViewNode.Checked"/> ändert.
      /// </summary>
      public event EventHandler<TreeViewNodeStatusChangedEventArgs> OnBeforeCheckedChanged;

      /// <summary>
      /// Bei einem <see cref="TreeViewNode"/> ändert sich <see cref="TreeViewNode.Checked"/>.
      /// </summary>
      public event EventHandler<TreeViewNodeEventArgs> OnCheckedChanged;

      /// <summary>
      /// Bevor sich bei einem <see cref="TreeViewNode"/> <see cref="TreeViewNode.Expanded"/> ändert.
      /// </summary>
      public event EventHandler<TreeViewNodeStatusChangedEventArgs> OnBeforeExpandedChanged;

      /// <summary>
      /// Bei einem <see cref="TreeViewNode"/> ändert sich <see cref="TreeViewNode.Expanded"/>.
      /// </summary>
      public event EventHandler<TreeViewNodeEventArgs> OnExpandedChanged;

      /// <summary>
      /// Auf einen <see cref="TreeViewNode"/> wurde getippt.
      /// </summary>
      public event EventHandler<TreeViewNodeEventArgs> OnNodeTapped;

      /// <summary>
      /// Auf einen <see cref="TreeViewNode"/> wurde doppelt getippt.
      /// </summary>
      public event EventHandler<TreeViewNodeEventArgs> OnNodeDoubleTapped;

      /// <summary>
      /// Ein <see cref="TreeViewNode"/> wurde gewischt.
      /// </summary>
      public event EventHandler<TreeViewNodeEventArgs> OnNodeSwiped;

      /// <summary>
      /// Der ausgewählte Node hat sich geändert.
      /// </summary>
      public event EventHandler<TreeViewNodeChangedEventArgs> OnSelectedNodeChanged;

      #endregion

      #region  Binding-Var Textcolor

      public static readonly BindableProperty TextcolorProperty = BindableProperty.Create(
         nameof(Textcolor),
         typeof(Color),
         typeof(Color),
         Color.Black);

      /// <summary>
      /// Textfarbe eines TreeNode-Items
      /// </summary>
      public Color Textcolor {
         get => (Color)GetValue(TextcolorProperty);
         set => SetValue(TextcolorProperty, value);
      }

      #endregion

      #region  Binding-Var TextColorSelectedNode

      public static readonly BindableProperty TextColorSelectedNodeProperty = BindableProperty.Create(
         nameof(TextColorSelectedNode),
         typeof(Color),
         typeof(Color),
         Color.White);

      /// <summary>
      /// Textfarbe eines ausgewählten TreeNode-Items
      /// </summary>
      public Color TextColorSelectedNode {
         get => (Color)GetValue(TextColorSelectedNodeProperty);
         set => SetValue(TextColorSelectedNodeProperty, value);
      }

      #endregion

      #region  Binding-Var BackcolorSelectedNode

      public static readonly BindableProperty BackcolorSelectedNodeProperty = BindableProperty.Create(
         nameof(BackcolorSelectedNode),
         typeof(Color),
         typeof(Color),
         Color.DarkGray);

      /// <summary>
      /// Hintergrundfarbe eines ausgewählten TreeNode-Items
      /// </summary>
      public Color BackcolorSelectedNode {
         get => (Color)GetValue(BackcolorSelectedNodeProperty);
         set => SetValue(BackcolorSelectedNodeProperty, value);
      }

      #endregion

      #region  Binding-Var ChildNodeIndent

      public static readonly BindableProperty ChildNodeIndentMarginProperty = BindableProperty.Create(
         nameof(ChildNodeIndent),
         typeof(Thickness),
         typeof(Thickness),
         new Thickness(50, 0, 0, 0));

      public Thickness ChildNodeIndentMargin {
         get => (Thickness)GetValue(ChildNodeIndentMarginProperty);
         set => SetValue(ChildNodeIndentMarginProperty, value);
      }

      /// <summary>
      /// linker Einzug für den Child-Bereich
      /// </summary>
      public double ChildNodeIndent {
         get => ChildNodeIndentMargin.Left;
         set => SetValue(ChildNodeIndentMarginProperty, new Thickness(value, 0, 0, 0));  // ChildIndentMargin = new Thickness(value, 0, 0, 0);
      }

      #endregion

      #region  Binding-Var FontSize

      public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
         nameof(FontSize),
         typeof(double),
         typeof(double),
         new FontSizeConverter().ConvertFromInvariantString("Medium"));

      /// <summary>
      /// Textgröße
      /// </summary>
      public double FontSize {
         get => (double)GetValue(FontSizeProperty);
         set => SetValue(FontSizeProperty, value);
      }

      #endregion


      StackLayout XamlChildContainer {
         get => treeViewStackLayout;
      }

      TreeViewNode selectedNode;
      public TreeViewNode SelectedNode {
         get => selectedNode;
         set {
            if (selectedNode == null ||
                !selectedNode.Equals(value)) {
               TreeViewNode oldSelectedNode = SelectedNode;
               if (oldSelectedNode != null) {
                  selectedNode.BackcolorNode = BackgroundColor;
                  selectedNode.BackcolorText = BackgroundColor;
                  selectedNode.Textcolor = Textcolor;
               }
               selectedNode = value;

               if (selectedNode != null) {
                  selectedNode.BackcolorNode = BackcolorSelectedNode;
                  selectedNode.BackcolorText = BackcolorSelectedNode;
                  selectedNode.Textcolor = TextColorSelectedNode;
               }

               OnSelectedNodeChanged?.Invoke(this, new TreeViewNodeChangedEventArgs(selectedNode, oldSelectedNode));
            }
         }
      }


      /// <summary>
      /// Anzahl der Child-Nodes
      /// </summary>
      public int ChildNodes {
         get => XamlChildContainer.Children != null ?
                  XamlChildContainer.Children.Count :
                  0;
      }

      /// <summary>
      /// Gibt es Child-Nodes?
      /// </summary>
      public bool HasChildNodes {
         get => ChildNodes > 0;
      }

      ///// <summary>
      ///// Child-Nodes sichtbar?
      ///// </summary>
      //public bool Expanded {
      //   get => XamlChildContainer.IsVisible;
      //   set {
      //      if (XamlChildContainer.IsVisible != value) {
      //         bool change = false;
      //         if (value) {
      //            if (HasChildNodes)
      //               change = true;
      //         } else
      //            change = true;

      //         if (change) {
      //            TreeViewNodeStatusChangedEventArgs args = new TreeViewNodeStatusChangedEventArgs(null);
      //            OnBeforeExpandedChanged?.Invoke(this, args);
      //            if (!args.Cancel) {
      //               XamlChildContainer.IsVisible = value;
      //               OnExpandedChanged?.Invoke(this, new TreeViewNodeEventArgs(null));
      //            }
      //         }
      //      }
      //   }
      //}


      public TreeView() {
         InitializeComponent();
      }


      /// <summary>
      /// liefert alle Child-Nodes im Container
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <returns></returns>
      public static List<TreeViewNode> GetChildNodes(StackLayout xamlChildContainer) {
         List<TreeViewNode> result = new List<TreeViewNode>();
         foreach (View item in xamlChildContainer.Children) {
            if (item is TreeViewNode)
               result.Add(item as TreeViewNode);
         }
         return result;
      }

      /// <summary>
      /// liefert alle Child-Nodes
      /// </summary>
      /// <returns></returns>
      public List<TreeViewNode> GetChildNodes() {
         return GetChildNodes(XamlChildContainer);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> an dieser Position im Container wird geliefert.
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="idx"></param>
      /// <returns></returns>
      public static TreeViewNode GetChildNode(StackLayout xamlChildContainer, int idx) {
         return isChildIdxValid(xamlChildContainer, idx) ?
                     xamlChildContainer.Children[idx] as TreeViewNode :
                     null;
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> an dieser Position wird geliefert.
      /// </summary>
      /// <param name="idx"></param>
      /// <returns></returns>
      public TreeViewNode GetChildNode(int idx) {
         return GetChildNode(XamlChildContainer, idx);
      }

      /// <summary>
      /// liefert den Index des <see cref="TreeViewNode"/> (oder -1)
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="tn"></param>
      /// <returns></returns>
      public static int GetChildIndex(StackLayout xamlChildContainer, TreeViewNode tn) {
         for (int i = 0; i < xamlChildContainer.Children.Count; i++)
            if (xamlChildContainer.Children[i] is TreeViewNode &&
                tn.Equals(xamlChildContainer.Children[i]))
               return i;
         return -1;
      }

      /// <summary>
      /// liefert den Index des <see cref="TreeViewNode"/> (oder -1)
      /// </summary>
      /// <param name="tn"></param>
      /// <returns></returns>
      public int GetChildIndex(TreeViewNode tn) {
         return GetChildIndex(XamlChildContainer, tn);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird im Container angehängt
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="node"></param>
      public static void AddChildNode(StackLayout xamlChildContainer, TreeViewNode node) {
         xamlChildContainer.Children.Add(node);
         SetPropsFromTreeView(node.TreeView, node);
         node.TreeView?.registerTreeViewNodeEvents(node);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird angehängt
      /// </summary>
      /// <param name="node"></param>
      public void AddChildNode(TreeViewNode node) {
         AddChildNode(XamlChildContainer, node);
      }

      /// <summary>
      /// Ein <see cref="TreeViewNode"/> für den Text wird erzeugt und angehängt.
      /// </summary>
      /// <param name="text"></param>
      /// <param name="extdata"></param>
      /// <returns></returns>
      public TreeViewNode AddChildNode(string text, object extdata = null) {
         TreeViewNode tn = new TreeViewNode(text, extdata);
         AddChildNode(tn);
         return tn;
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird an der Position im Container eingefügt.
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="pos"></param>
      /// <param name="node"></param>
      public static void InsertChildNode(StackLayout xamlChildContainer, int pos, TreeViewNode node) {
         xamlChildContainer.Children.Insert(pos, node);
         SetPropsFromTreeView(node.TreeView, node);
         node.TreeView?.registerTreeViewNodeEvents(node);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird an der Position eingefügt.
      /// </summary>
      /// <param name="pos"></param>
      /// <param name="node"></param>
      public void InsertChildNode(int pos, TreeViewNode node) {
         InsertChildNode(XamlChildContainer, pos, node);
      }

      /// <summary>
      /// Ein <see cref="TreeViewNode"/> für den Text wird erzeugt und eingefügt.
      /// </summary>
      /// <param name="pos"></param>
      /// <param name="text"></param>
      /// <param name="extdata"></param>
      /// <returns></returns>
      public TreeViewNode InsertChildNode(int pos, string text, object extdata = null) {
         TreeViewNode tn = new TreeViewNode(text, extdata);
         InsertChildNode(pos, tn);
         return tn;
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> an der Position im Container wird entfernt.
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="pos"></param>
      public static void RemoveChildNode(StackLayout xamlChildContainer, int pos) {
         if (isChildIdxValid(xamlChildContainer, pos))
            xamlChildContainer.Children.RemoveAt(pos);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> an der Position wird entfernt.
      /// </summary>
      /// <param name="pos"></param>
      public void RemoveChildNode(int pos) {
         RemoveChildNode(XamlChildContainer, pos);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird im Container entfernt.
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="node"></param>
      /// <returns></returns>
      public static bool RemoveChildNode(StackLayout xamlChildContainer, TreeViewNode node) {
         return xamlChildContainer.Children.Remove(node);
      }

      /// <summary>
      /// Der <see cref="TreeViewNode"/> wird entfernt.
      /// </summary>
      /// <param name="node"></param>
      /// <returns></returns>
      public bool RemoveChildNode(TreeViewNode node) {
         return RemoveChildNode(XamlChildContainer, node);
      }

      /// <summary>
      /// Alle <see cref="TreeViewNode"/> werden entfernt.
      /// </summary>
      public static void RemoveChildNodes(StackLayout xamlChildContainer) {
         xamlChildContainer.Children.Clear();
      }

      /// <summary>
      /// Alle <see cref="TreeViewNode"/> werden entfernt.
      /// </summary>
      public void RemoveChildNodes() {
         RemoveChildNodes(XamlChildContainer);
      }

      /// <summary>
      /// Ist idx ein gültiger Index?
      /// </summary>
      /// <param name="xamlChildContainer"></param>
      /// <param name="idx"></param>
      /// <returns></returns>
      static bool isChildIdxValid(StackLayout xamlChildContainer, int idx) {
         return xamlChildContainer != null &&
                xamlChildContainer.Children != null &&
                0 <= idx &&
                idx < xamlChildContainer.Children.Count;
      }

      public static void SetPropsFromTreeView(TreeView tv, TreeViewNode node) {
         if (tv != null) {
            node.BackcolorNode = tv.BackgroundColor;
            node.BackcolorText = tv.BackgroundColor;
            node.Textcolor = tv.Textcolor;
         }
      }

      void registerTreeViewNodeEvents(TreeViewNode node) {
         node.OnTapped += Node_OnTapped;
         node.OnDoubleTapped += Node_OnDoubleTapped;

         node.OnSwipe += Node_OnSwipe;

         node.OnBeforeExpandedChanged += Node_OnBeforeExpandedChanged;
         node.OnExpandedChanged += Node_OnExpandedChanged;

         node.OnBeforeCheckedChanged += Node_OnBeforeCheckedChanged;
         node.OnCheckedChanged += Node_OnCheckedChanged;
      }

      private void Node_OnBeforeCheckedChanged(object sender, TreeViewNode.BoolResultEventArgs e) {
         TreeViewNodeStatusChangedEventArgs args = new TreeViewNodeStatusChangedEventArgs(sender as TreeViewNode);
         OnBeforeCheckedChanged?.Invoke(this, args);
         e.Cancel = args.Cancel;
      }

      private void Node_OnCheckedChanged(object sender, EventArgs e) {
         OnCheckedChanged?.Invoke(this, new TreeViewNodeEventArgs(sender as TreeViewNode));
      }

      private void Node_OnBeforeExpandedChanged(object sender, TreeViewNode.BoolResultEventArgs e) {
         TreeViewNodeStatusChangedEventArgs args = new TreeViewNodeStatusChangedEventArgs(sender as TreeViewNode);
         OnBeforeExpandedChanged?.Invoke(this, args);
         e.Cancel = args.Cancel;
      }

      private void Node_OnExpandedChanged(object sender, EventArgs e) {
         OnExpandedChanged?.Invoke(this, new TreeViewNodeEventArgs(sender as TreeViewNode));
      }

      private void Node_OnSwipe(object sender, EventArgs e) {
         OnNodeSwiped?.Invoke(this, new TreeViewNodeEventArgs(sender as TreeViewNode));
      }

      private void Node_OnDoubleTapped(object sender, EventArgs e) {
         OnNodeDoubleTapped?.Invoke(this, new TreeViewNodeEventArgs(sender as TreeViewNode));
      }

      private void Node_OnTapped(object sender, EventArgs e) {
         OnNodeTapped?.Invoke(this, new TreeViewNodeEventArgs(sender as TreeViewNode));
      }

      /// <summary>
      /// einige Propertie-Änderungen für alle <see cref="TreeViewNode"/> übernehmen
      /// </summary>
      /// <param name="propertyName"></param>
      protected override void OnPropertyChanged([CallerMemberName] string propertyName = null) {
         base.OnPropertyChanged(propertyName);

         if (propertyName == nameof(BackgroundColor) ||
             propertyName == nameof(Textcolor) ||
             //propertyName == nameof(BackcolorSelectedNode) ||
             //propertyName == nameof(ColorSelectedText) ||
             propertyName == nameof(FontSize) ||
             propertyName == nameof(ChildNodeIndent)) {
            changeProperty4Childs(propertyName, GetChildNodes());
         }
      }

      /// <summary>
      /// ein Propertie vom <see cref="TreeView"/> auf die Liste der <see cref="TreeViewNode"/> (und ihre Childs) übernehmen
      /// </summary>
      /// <param name="propertyName"></param>
      /// <param name="nodes"></param>
      void changeProperty4Childs(string propertyName, IList<TreeViewNode> nodes) {
         foreach (TreeViewNode node in nodes) {
            if (propertyName == nameof(BackgroundColor)) {

               node.BackgroundColor =
               node.BackcolorNode =
               node.BackcolorText = BackgroundColor;

            } else if (propertyName == nameof(Textcolor)) {

               node.Textcolor = Textcolor;

            } else if (propertyName == nameof(ChildNodeIndent)) {

               node.ChildNodeIndent = ChildNodeIndent;

            } else if (propertyName == nameof(FontSize)) {

               node.FontSize = FontSize;

            }

            changeProperty4Childs(propertyName, node.GetChildNodes());
         }
      }

      /// <summary>
      /// bis zum angegebenen <see cref="TreeViewNode"/> "aufklappen"
      /// </summary>
      /// <param name="tn"></param>
      public void ExpandToNode(TreeViewNode tn) {
         if (tn != null) {
            TreeViewNode tnparent = tn.ParentNode;
            while (tnparent != null) {
               if (!tnparent.Expanded)
                  tnparent.Expanded = true;
               tnparent = tnparent.ParentNode;
            }
         }
      }

      /// <summary>
      /// der angegebene <see cref="TreeViewNode"/>  wird auf jeden Fall sichtbar
      /// </summary>
      /// <param name="tn"></param>
      public void EnsureVisibleNode(TreeViewNode tn) {
         if (tn != null) {
            // bei Bedarf erstmal "ausklappen"
            ExpandToNode(tn);
            srollToCenter(tn.XamlChildContainer);
         }
      }

      async static void srollToCenter(Element el) {
         Element parent = el.Parent;
         while (parent != null && !(parent is ScrollView)) {
            parent = parent.Parent;
         }
         if (parent != null &&
             parent is ScrollView)
            await (parent as ScrollView).ScrollToAsync(el, ScrollToPosition.Center, false);
      }


   }
}