﻿using System.ComponentModel.Composition;
using System.Web.Script.Serialization;
using ICSharpCode.AvalonEdit.Document;

namespace RazorPad.Providers
{
    public class JsonModelProvider : ModelProvider
    {
        public string Json
        {
            get { return _json; }
            set
            {
                if (_json == value)
                    return;

                _json = value;
                TriggerModelChanged();
            }
        }
        private string _json;

        public JsonModelProvider(string json = null)
        {
            Json = json;

            if (!string.IsNullOrWhiteSpace(json))
                JsonDocument = new TextDocument(json);
            else
            {
                JsonDocument = new TextDocument();
            }
        }

        protected TextDocument JsonDocument
        {
            get;
            set;
        }

        public override string Serialize()
        {
            return Json;
        }

        public override void Deserialize(string serialized)
        {
            Json = serialized;
        }

        protected override dynamic RebuildModel()
        {
            var serializer = new JavaScriptSerializer();

            var json = (string.IsNullOrWhiteSpace(Json)) ? "{}" : Json;
            return serializer.DeserializeObject(json);
        }


        [Export(typeof(IModelProviderFactory))]
        public class JsonModelProviderFactory : IModelProviderFactory
        {
            public IModelProvider Create(dynamic model = null)
            {
                return new JsonModelProvider(json: model);
            }
        }
    }
}