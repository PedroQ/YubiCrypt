using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace YubiCrypt.Mobile.WindowsPhone.Data
{
    public sealed class YCDataSource
    {
        private static YCDataSource _ycDataSource = new YCDataSource();
        private ObservableCollection<YCFileData> _contents = new ObservableCollection<YCFileData>();

        public ObservableCollection<YCFileData> Contents
        {
            get { return this._contents; }
        }

        public static async Task<IEnumerable<YCFileData>> GetContentsAsync()
        {
            await _ycDataSource.GetSampleDataAsync();

            return _ycDataSource.Contents;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._contents.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/YCSampleData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Contents"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                YCFileData group = new YCFileData(groupObject["FileName"].GetString(),
                                                       groupObject["Size"].GetString(),
                                                       FFSJSON(groupObject["Modified"].GetString()),
                                                       groupObject["InternalName"].GetString());

                this.Contents.Add(group);
            }
        }

        private DateTime FFSJSON(string jsonDatetime)
        {
            var match = Regex.Match(jsonDatetime, @"/Date\((?<millisecs>-?\d*)\)/");
            var millisecs = Convert.ToInt64(match.Groups["millisecs"].Value);
            var date = new DateTime(1970, 1, 1).AddMilliseconds(millisecs);
            return date;
        }
    }

    public class YCFileData
    {
        public YCFileData(string fileName, string size, DateTime modified, string internalName)
        {
            this.FileName = fileName;
            this.Size = size;
            this.Modified = modified;
            this.InternalName = internalName;
        }

        public string FileName { get; set; }
        public string Size { get; set; }
        public DateTime Modified { get; set; }
        public string InternalName { get; set; }
    }
}
