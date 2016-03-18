﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using AutoMapper;

using FirstFloor.ModernUI.Windows.Controls;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using SC2LiquipediaStatistics.DesktopClient.Model;
using SC2LiquipediaStatistics.DesktopClient.Service;
using SC2LiquipediaStatistics.DesktopClient.View;
using SC2LiquipediaStatistics.Utilities.DataBase;
using SC2LiquipediaStatistics.Utilities.Domain;

using SC2Statistics.SC2Domain.Service;

using SC2DomainEntities = SC2Statistics.SC2Domain.Model;

namespace SC2LiquipediaStatistics.DesktopClient.ViewModel
{
    public class EditEventViewModel : ModernViewModelBase
    {
        public ISC2Service SC2Service { get; private set; }

        public IParseService ParseService { get; private set; }

        public IModernNavigationService NavigationService { get; private set; }

        public IMapper Mapper { get; protected set; }

        public ILoadingService LoadingService { get; protected set; }

        private Event selectedEvent;
        public Event SelectedEvent
        {
            get
            {
                return selectedEvent;
            }
            set
            {
                if (selectedEvent == value || value == null)
                    return;

                Set(() => SelectedEvent, ref selectedEvent, value, true);
            }
        }

        private SubEvent selectedSubEvent;
        public SubEvent SelectedSubEvent
        {
            get
            {
                return selectedSubEvent;
            }
            set
            {
                if (selectedSubEvent == value || value == null)
                    return;

                Set(() => SelectedSubEvent, ref selectedSubEvent, value, true);
            }
        }

        public string subEventUrl;
        public string SubEventUrl
        {
            get
            {
                return subEventUrl;
            }
            set
            {
                if (subEventUrl == value || value == null)
                    return;

                Set(() => SubEventUrl, ref subEventUrl, value, true);
            }
        }

        public IList<KeyValuePair<string, SC2DomainEntities.Expansion>> Expansions { get; private set; }

        protected KeyValuePair<string, SC2DomainEntities.Expansion> selectedExpansion;
        public KeyValuePair<string, SC2DomainEntities.Expansion> SelectedExpansion
        {
            get
            {
                return selectedExpansion;
            }
            set
            {
                if (selectedExpansion.Value == value.Value)
                    return;

                Set(() => SelectedExpansion, ref selectedExpansion, value, true);
            }
        }

        public IEnumerable<SC2DomainEntities.LiquipediaTier> LiquipediaTiers
        {
            get
            {
                return Enum.GetValues(typeof(SC2DomainEntities.LiquipediaTier))
                    .Cast<SC2DomainEntities.LiquipediaTier>();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand ReloadAllEvenDataCommand { get; private set; }
        public ICommand ReloadMainEvenDataCommand { get; private set; }
        public ICommand EditSubEventCommand { get; private set; }
        public ICommand DeleteSubEventCommand { get; private set; }
        public ICommand AddNewSubEventCommand { get; private set; }

        public EditEventViewModel(ISC2Service sc2Service, IParseService parseService, IModernNavigationService navigationService, ILoadingService loadingService, IMapper mapper)
        {
            SC2Service = sc2Service;
            ParseService = parseService;
            NavigationService = navigationService;
            LoadingService = loadingService;
            Mapper = mapper;

            Expansions = new List<KeyValuePair<string, SC2DomainEntities.Expansion>>();
            Expansions.Add(new KeyValuePair<string, SC2DomainEntities.Expansion>("Hearth of the Swarm", SC2DomainEntities.Expansion.HeartOfTheSwarm));
            Expansions.Add(new KeyValuePair<string, SC2DomainEntities.Expansion>("Legacy of the Void", SC2DomainEntities.Expansion.LegacyOfTheVoid));
            Expansions.Add(new KeyValuePair<string, SC2DomainEntities.Expansion>("Wings of Liberty", SC2DomainEntities.Expansion.WingsOfLiberty));
            Expansions.Add(new KeyValuePair<string, SC2DomainEntities.Expansion>("Undefined", SC2DomainEntities.Expansion.Undefined));

            SaveCommand = new RelayCommand(SaveEvent);
            NavigatedToCommand = new RelayCommand<object>(SelectEvent);
            ReloadAllEvenDataCommand = new RelayCommand(ReloadAllEvent);
            ReloadMainEvenDataCommand = new RelayCommand(ReloadMainEvent);
            EditSubEventCommand = new RelayCommand(EditSubEvent);
            DeleteSubEventCommand = new RelayCommand(DeleteSelectedSubEvent);
            AddNewSubEventCommand = new RelayCommand(AddNewSubEvent);
        }

        private void ReloadMainEvent()
        {
            ValidationException validationException = null;

            LoadingService.ShowAndExecuteAction(delegate
            {
                try
                {
                    using (new NHibernateSessionContext())
                    {
                        var updatedEvent = ParseService.GetSC2Event(SelectedEvent.LiquipediaReference);
                        var domainEvent = SC2Service.LoadEvent(SelectedEvent.Id);
                        domainEvent.Merge(updatedEvent, true, false);
                        SC2Service.UpdateEvent(domainEvent);
                        SelectedEvent = Mapper.Map<SC2DomainEntities.Event, Event>(domainEvent);
                    }
                }
                catch (ValidationException ex)
                {
                    validationException = ex;
                }
            });

            if (validationException != null)
                ModernDialog.ShowMessage(validationException.GetFormatedMessage(), "Validation Message", MessageBoxButton.OK);
        }

        private void AddNewSubEvent()
        {
            ValidationException validationException = null;

            LoadingService.ShowAndExecuteAction(delegate
            {
                using (new NHibernateSessionContext())
                {
                    try
                    {
                        var subEvent = ParseService.GetSC2EventWithSubEvents(SubEventUrl);
                        var mainEvent = SC2Service.LoadEvent(SelectedEvent.Id);
                        mainEvent.AddSubEvent(subEvent);
                        SC2Service.UpdateEvent(mainEvent);
                    }
                    catch (ValidationException ex)
                    {
                        validationException = ex;
                    }
                }
            });

            if (validationException != null)
            {
                ModernDialog.ShowMessage(validationException.GetFormatedMessage(), "Validation Message", MessageBoxButton.OK);
                return;
            }

            SubEventUrl = String.Empty;
            ReloadSelectedEvent(SelectedEvent.Id);
        }

        private void EditSubEvent()
        {
            using (new NHibernateSessionContext())
            {
                var domainSubEvent = SC2Service.LoadEvent(SelectedSubEvent.Id);
                SelectedEvent = Mapper.Map<SC2DomainEntities.Event, Event>(domainSubEvent);
            }
        }

        private void ReloadAllEvent()
        {
            ValidationException validationException = null;

            LoadingService.ShowAndExecuteAction(delegate
            {
                try
                {
                    using (new NHibernateSessionContext())
                    {
                        
                        var updatedEvent = ParseService.GetSC2EventWithSubEvents(SelectedEvent.LiquipediaReference);
                        var domainEvent = SC2Service.LoadEvent(SelectedEvent.Id);
                        domainEvent.Merge(updatedEvent, true, true);
                        SC2Service.UpdateEvent(domainEvent);
                        SelectedEvent = Mapper.Map<SC2DomainEntities.Event, Event>(domainEvent);
                    }
                }
                catch (ValidationException ex)
                {
                    validationException = ex;
                }
            });

            if (validationException != null)
                ModernDialog.ShowMessage(validationException.GetFormatedMessage(), "Validation Message", MessageBoxButton.OK);
        }

        private void SelectEvent(object parameter)
        {
            SelectedEvent = parameter as Event;
            if (SelectedEvent != null)
            {
                SelectedExpansion = Expansions.First(x => x.Value == SelectedEvent.Expansion);
            }
        }

        private void SaveEvent()
        {
            using (new NHibernateSessionContext())
            {
                var domainEvent = SC2Service.LoadEvent(SelectedEvent.Id);
                domainEvent = Mapper.Map(SelectedEvent, domainEvent);
                
                // TODO: Update IsActive in the Grid
                //var eventsIdToActive = SelectedEvent.SubEvents
                //    .Where(x => x.IsActive)
                //    .Select(x => x.Id);

                //var eventsIdToDeactive = SelectedEvent.SubEvents
                //    .Where(x => !x.IsActive)
                //    .Select(x => x.Id);

                SC2Service.UpdateEvent(domainEvent);
            }

            if (SelectedEvent.MainEvent != null)
            {
                ReloadSelectedEvent(SelectedEvent.MainEvent.Id);
            }
            else
                NavigationService.GoBack();
        }

        private void DeleteSelectedSubEvent()
        {
            var result = ModernDialog.ShowMessage("Do you really want to delete this Sub Event?", "Attention", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                return;

            using (new NHibernateSessionContext())
            {
                SC2Service.DeleteSubEvent(SelectedEvent.Id, SelectedSubEvent.Id);
            }

            ReloadSelectedEvent(SelectedEvent.Id);
        }

        private void ReloadSelectedEvent(long selectedEventId)
        {
            using (new NHibernateSessionContext())
            {
                var domainEvent = SC2Service.LoadEvent(selectedEventId);
                SelectedEvent = Mapper.Map<SC2DomainEntities.Event, Event>(domainEvent);
            }
        }
    }
}
