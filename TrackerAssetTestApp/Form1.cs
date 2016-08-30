using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AssetPackage;

namespace TrackerAssetTestApp
{
    public partial class Form1 : Form
    {
        TrackerAsset tracker;
        TrackerAssetSettings settings;

        public Form1()
        {
            InitializeComponent();
        }

        public void UpdateTrackerAndConsole()
        {
            TrackerAsset.Instance.Flush();
            Console.WriteLine();
            ConsoleWindow.SelectionStart = ConsoleWindow.TextLength;
            ConsoleWindow.ScrollToCaret();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Console.SetOut(new TextBoxWritter(ConsoleWindow));
            TraceVerb.DataSource = Enum.GetValues(typeof(TrackerAsset.Verb));
            List<object> values = new List<object>();
            values.AddRange(Enum.GetValues(typeof(AccessibleTracker.Accessible)).Cast<object>());
            values.AddRange(Enum.GetValues(typeof(AlternativeTracker.Alternative)).Cast<object>());
            values.AddRange(Enum.GetValues(typeof(CompletableTracker.Completable)).Cast<object>());
            values.AddRange(Enum.GetValues(typeof(GameObjectTracker.TrackedGameObject)).Cast<object>());

            List<KeyValuePair<int, string>> boolean_values = new List<KeyValuePair<int, string>>();
            boolean_values.Add(new KeyValuePair<int, string>(-1, "Undefined"));
            boolean_values.Add(new KeyValuePair<int, string>(0, "False"));
            boolean_values.Add(new KeyValuePair<int, string>(1, "True"));

            TraceObjectType.DataSource = values;
            TraceResultSucced.DataSource = new List<KeyValuePair<int,string>>(boolean_values);
            TraceResultCompleted.DataSource = new List<KeyValuePair<int, string>>(boolean_values);
            CompletableSucced.DataSource = new List<KeyValuePair<int, string>>(boolean_values);

            AccesibleType.DataSource = Enum.GetValues(typeof(AccessibleTracker.Accessible));
            AlternativeType.DataSource = Enum.GetValues(typeof(AlternativeTracker.Alternative));
            CompletableType.DataSource = Enum.GetValues(typeof(CompletableTracker.Completable));
            GameObjectType.DataSource = Enum.GetValues(typeof(GameObjectTracker.TrackedGameObject));


            settings = new TrackerAssetSettings();
            settings.Host = "127.0.0.1";
            settings.Port = 8080;
            settings.StorageType = TrackerAsset.StorageTypes.local;
            settings.TraceFormat = TrackerAsset.TraceFormats.csv;

            TrackerAsset.Instance.Bridge = new TesterBridge();

            //TrackerAsset.Instance.Login("teacher", "teacher");

            TrackerAsset.Instance.Start();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {

            TrackerAsset.Verb verb;
            Enum.TryParse<TrackerAsset.Verb>(TraceVerb.SelectedValue.ToString(), out verb);
            TrackerAsset.TrackerEvent trace = new TrackerAsset.TrackerEvent()
            {

                Target = new TrackerAsset.TrackerEvent.TraceObject(TraceObjectType.SelectedValue.ToString().ToLower(), TraceObjectId.Text),
                Event = new TrackerAsset.TrackerEvent.TraceVerb(verb)
            };

            int success = ((KeyValuePair<int, string>)TraceResultSucced.SelectedValue).Key,
                completed = ((KeyValuePair<int, string>)TraceResultCompleted.SelectedValue).Key;

            if(success != -1 || completed != -1 || TraceResultScoreSend.Checked || TraceResultResponse.Text != "")
            {
                TrackerAsset.TrackerEvent.TraceResult result = new TrackerAsset.TrackerEvent.TraceResult();
                switch (success)
                {
                    case 0: result.Success = false; break;
                    case 1: result.Success = true; break;
                    case -1:
                    default: break;
                }

                switch (completed)
                {
                    case 0: result.Completion = false; break;
                    case 1: result.Completion = true; break;
                    case -1:
                    default: break;
                }

                if(TraceResultResponse.Text != "")
                {
                    result.Response = TraceResultResponse.Text;
                }

                if (TraceResultScoreSend.Checked)
                {
                    result.Score = (float) TraceResultScore.Value;
                }

                trace.Result = result;
            }


            TrackerAsset.Instance.Trace(trace);
            UpdateTrackerAndConsole();
        }

        private void TraceCSV_CheckedChanged(object sender, EventArgs e)
        {
            if (TraceCSV.Checked)
            {
                settings.TraceFormat = TrackerAsset.TraceFormats.csv;
                TrackerAsset.Instance.Settings = settings;
            }
        }

        private void TraceJSON_CheckedChanged(object sender, EventArgs e)
        {
            if (TraceJSON.Checked)
            {
                settings.TraceFormat = TrackerAsset.TraceFormats.json;
                TrackerAsset.Instance.Settings = settings;
            }
        }

        private void TraceXAPI_CheckedChanged(object sender, EventArgs e)
        {
            if (TraceXAPI.Checked)
            {
                settings.TraceFormat = TrackerAsset.TraceFormats.xapi;
                TrackerAsset.Instance.Settings = settings;
            }
        }

        private void TraceXML_CheckedChanged(object sender, EventArgs e)
        {
            if (TraceXML.Checked)
            {
                settings.TraceFormat = TrackerAsset.TraceFormats.xml;
                TrackerAsset.Instance.Settings = settings;
            }
        }

        private void AddVariable_Click(object sender, EventArgs e)
        {
            if (TraceVarName.Text != "")
                TrackerAsset.Instance.setExtension(TraceVarName.Text, TraceVarValue.Text);
        }

        private void AccesibleAccessed_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Accesible.Accessed(AccesibleId.Text, (AccessibleTracker.Accessible)AccesibleType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void AccesibleSkipped_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Accesible.Skipped(AccesibleId.Text, (AccessibleTracker.Accessible)AccesibleType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void AlternativeSelected_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Alternative.Selected(AlternativeId.Text, AlternativeOption.Text, (AlternativeTracker.Alternative)AlternativeType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void AlternativeUnlocked_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Alternative.Unlocked(AlternativeId.Text, AlternativeOption.Text, (AlternativeTracker.Alternative)AlternativeType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void GameObjectInteracted_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.GameObject.Interacted(GameObjectId.Text, (GameObjectTracker.TrackedGameObject)GameObjectType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void GameObjectUsed_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.GameObject.Used(GameObjectId.Text, (GameObjectTracker.TrackedGameObject)GameObjectType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void CompletableInitialized_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Completable.Initialized(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue);
            UpdateTrackerAndConsole();
        }

        private void CompletableProgressed_Click(object sender, EventArgs e)
        {
            TrackerAsset.Instance.Completable.Progressed(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue, (float) CompletableNumeric.Value);
            UpdateTrackerAndConsole();
        }

        private void CompletableCompleted_Click(object sender, EventArgs e)
        {
            int success = ((KeyValuePair<int, string>)CompletableSucced.SelectedValue).Key;

            if (success != -1 && CompletableNumericCheck.Checked)
            {
                TrackerAsset.Instance.Completable.Completed(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue, success == 0 ? false:true , (float)CompletableNumeric.Value);
            }
            else if(success != -1)
            {
                TrackerAsset.Instance.Completable.Completed(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue, success == 0 ? false : true);
            }
            else if (CompletableNumericCheck.Checked)
            {
                TrackerAsset.Instance.Completable.Completed(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue, (float)CompletableNumeric.Value);
            }else
            {
                TrackerAsset.Instance.Completable.Completed(CompletableId.Text, (CompletableTracker.Completable)CompletableType.SelectedValue);
            }

            UpdateTrackerAndConsole();
        }
    }
}
