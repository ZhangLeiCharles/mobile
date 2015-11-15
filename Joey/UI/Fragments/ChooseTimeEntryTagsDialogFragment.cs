using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;

namespace Toggl.Joey.UI.Fragments
{
    public interface IUpdateTagList
    {
        void OnCreateNewTag (TagData newTagData);

        void OnModifyTagList (List<TagData> newTagList);
    }

    public class ChooseTimeEntryTagsDialogFragment : BaseDialogFragment
    {
        private static readonly string SelectedTagNamesArgument = "com.toggl.timer.selected_tag_names";
        private static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";
        private ListView listView;
        private TagListViewModel viewModel;
        private IUpdateTagList updateTagHandler;

        private Guid WorkspaceId
        {
            get {
                Guid id;
                Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                return id;
            }
        }

        private IList<string> ExistingTags
        {
            get {
                return Arguments.GetStringArrayList (SelectedTagNamesArgument);
            }
        }

        public ChooseTimeEntryTagsDialogFragment ()
        {
        }

        public ChooseTimeEntryTagsDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static ChooseTimeEntryTagsDialogFragment NewInstance (Guid workspaceId, List<string> tagNames)
        {
            var fragment = new ChooseTimeEntryTagsDialogFragment ();

            var args = new Bundle ();
            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            args.PutStringArrayList (SelectedTagNamesArgument, tagNames);
            fragment.Arguments = args;

            return fragment;
        }

        public async override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            viewModel = new TagListViewModel (WorkspaceId);
            await viewModel.Init ();
            SetPreviousSelectedTags ();
        }

        public override void OnDestroy ()
        {
            viewModel.Dispose ();
            base.OnDestroy ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            // Mvvm ligth utility to generate an adapter from
            // an Observable collection.
            var tagsAdapter = viewModel.TagCollection.GetAdapter (GetTagView);

            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.ChooseTimeEntryTagsDialogTitle)
            .SetAdapter (tagsAdapter, (IDialogInterfaceOnClickListener)null)
            .SetNegativeButton (Resource.String.ChooseTimeEntryTagsDialogCancel, OnCancelButtonClicked)
            .SetPositiveButton (Resource.String.ChooseTimeEntryTagsDialogOk, OnOkButtonClicked)
            .SetNeutralButton (Resource.String.ChooseTimeEntryTagsDialogCreate, OnCreateButtonClicked)
            .Create ();

            listView = dia.ListView;
            listView.ItemsCanFocus = false;
            listView.ChoiceMode = ChoiceMode.Multiple;
            SetPreviousSelectedTags ();

            return dia;
        }

        public ChooseTimeEntryTagsDialogFragment SetOnModifyTagListHandler (IUpdateTagList handler)
        {
            updateTagHandler = handler;
            return this;
        }

        private void OnCreateButtonClicked (object sender, DialogClickEventArgs args)
        {
            // Commit changes the user has made thusfar
            if (updateTagHandler != null) {
                updateTagHandler.OnModifyTagList (SelectedTags);
            }

            CreateTagDialogFragment.NewInstance (WorkspaceId)
            .SetCreateNewTagHandler (updateTagHandler)
            .Show (FragmentManager, "new_tag_dialog");

            Dismiss ();
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            updateTagHandler.OnModifyTagList (SelectedTags);
        }

        private View GetTagView (int position, TagData tagData, View convertView)
        {
            View view = convertView ?? LayoutInflater.FromContext (Activity).Inflate (Resource.Layout.TagListItem, null);
            var nameCheckedTextView = view.FindViewById<CheckedTextView> (Resource.Id.NameCheckedTextView).SetFont (Font.Roboto);
            nameCheckedTextView.Text = tagData.Name;
            return view;
        }

        private List<TagData> SelectedTags
        {
            get {
                var list = new List<TagData> ();
                for (int i = 0; i < viewModel.TagCollection.Count; i++) {
                    if (listView.CheckedItemPositions.Get (i)) {
                        list.Add (viewModel.TagCollection [i]);
                    }

                }
                return list;
            }
        }

        private void SetPreviousSelectedTags ()
        {
            if (viewModel.TagCollection == null || listView == null) {
                return;
            }

            var list = ExistingTags;
            listView.ClearChoices ();
            for (int i = 0; i < viewModel.TagCollection.Count; i++) {
                if (list.Contains (viewModel.TagCollection [i].Name)) {
                    listView.SetSelection (i + 1);
                }
            }
        }
    }
}
