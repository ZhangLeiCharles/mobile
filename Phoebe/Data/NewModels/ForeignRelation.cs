﻿using System;
using System.ComponentModel;

namespace Toggl.Phoebe.Data.NewModels
{
    internal class ForeignRelation<T>
        where T : class, IModel
    {
        private T model;

        public ForeignRelation ()
        {
            Required = true;
        }

        public bool Required { get; set; }

        public Func<Guid, T> Factory { get; set; }

        public Action<Guid?> Changed { get; set; }

        public Action ShouldLoad { get; set; }

        public bool IsNewInstance { get; private set; }

        public T Get (Guid? foreignKey)
        {
            if (Required && foreignKey == null)
                throw new ArgumentNullException ("value");

            if (ShouldLoad != null)
                ShouldLoad ();

            if (model == null || model.Id != foreignKey) {
                // Unregister event listener
                if (model != null) {
                    model.PropertyChanged -= OnModelPropertyChanged;
                }

                if (foreignKey.HasValue) {
                    model = Factory (foreignKey.Value);
                    model.PropertyChanged += OnModelPropertyChanged;
                }
            }

            return model;
        }

        public void Set (T value)
        {
            if (Required && value == null)
                throw new ArgumentNullException ("value");

            if (ShouldLoad != null)
                ShouldLoad ();

            // Unregister event listener
            if (model != null) {
                model.PropertyChanged -= OnModelPropertyChanged;
            }

            model = value;
            if (model != null) {
                model.PropertyChanged += OnModelPropertyChanged;
            }

            IsNewInstance = true;
            try {
                OnChanged ();
            } finally {
                IsNewInstance = false;
            }
        }

        private void OnModelPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == WorkspaceModel.PropertyId) {
                OnChanged ();
            }
        }

        private void OnChanged ()
        {
            if (Changed == null)
                return;

            if (model == null) {
                if (Required)
                    throw new InvalidOperationException ("Cannot update required foreign Id when model unset.");
                Changed (null);
            } else {
                Changed (model.Id);
            }
        }
    }
}
