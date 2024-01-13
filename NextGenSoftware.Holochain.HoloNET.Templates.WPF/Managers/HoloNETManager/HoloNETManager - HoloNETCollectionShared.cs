﻿using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Collections;
using NextGenSoftware.Holochain.HoloNET.Templates.WPF.Enums;
using NextGenSoftware.Holochain.HoloNET.Templates.WPF.Models;
using NextGenSoftware.Holochain.HoloNET.Templates.WPF.UserControls;

namespace NextGenSoftware.Holochain.HoloNET.Templates.WPF.Managers
{
    public partial class HoloNETManager
    {
        public void InitHoloNETCollection(HoloNETClient client)
        {
            HoloNETManager.Instance.LogMessage("APP: Initializing HoloNET Collection...");
            HoloNETManager.Instance.ShowStatusMessage("Initializing HoloNET Collection...", StatusMessageType.Information, true);

            if (!HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name) || (HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name) && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] == null))
            {
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] = new HoloNETObservableCollection<AvatarShared>("oasis", "load_avatars", "add_avatar", "remove_avatar", client, "update_avatars");
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnInitialized += HoloNETEntriesShared_OnInitialized;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnCollectionLoaded += HoloNETEntriesShared_OnCollectionLoaded;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnCollectionSaved += HoloNETEntriesShared_OnCollectionSaved;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].CollectionChanged += HoloNETEntriesShared_CollectionChanged;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnHoloNETEntryAddedToCollection += HoloNETEntriesShared_OnHoloNETEntryAddedToCollection;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnHoloNETEntryRemovedFromCollection += HoloNETEntriesShared_OnHoloNETEntryRemovedFromCollection;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnClosed += HoloNETEntriesShared_OnClosed;
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].OnError += HoloNETEntriesShared_OnError;
            }
            else
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].HoloNETClient = client;
        }

        public async Task LoadCollection(HoloNETClient client)
        {
            if (!HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name)
                || (HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name) && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] == null)
                || (HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name) && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] != null && !HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].IsInitialized))
                HoloNETManager.Instance.InitHoloNETCollection(client);

            else if (HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].HoloNETClient.EndPoint != client.EndPoint)
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].HoloNETClient = client;

            else
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Already Initialized.", StatusMessageType.Success, false);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Already Initialized..");
            }

            if (HoloNETManager.Instance.HoloNETEntriesShared.ContainsKey(HoloNETManager.Instance.CurrentApp.Name) && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] != null)
            {
                HoloNETManager.Instance.ShowStatusMessage($"Loading HoloNET Collection...", StatusMessageType.Information, true, HoloNETEntryUIManager.CurrentHoloNETEntryUI);
                HoloNETManager.Instance.LogMessage($"APP: Loading HoloNET Collection...");

                //Non-async way (you need to use the OnCollectionLoaded event to know when the collection has loaded and receive the data/collection.
                //For non-async methods you will need to wait till the OnInitialized event to raise before calling any other methods such as the one below...
                //HoloNETEntries[CurrentApp.Name].LoadCollection(); 

                //LoadCollectionAsync (as well as all other async methods) will automatically init and wait for the client to finish connecting and retreiving agentPubKey (if needed) and raising the OnInitialized event.
                HoloNETCollectionLoadedResult<AvatarShared> result = await HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].LoadCollectionAsync(); //No event handlers are needed but you can still use if you like.
                HandleHoloNETCollectionLoaded(result);
            }
        }

        public async Task AddHoloNETEntryToCollection(HoloNETClient client, string firstName, string lastName, DateTime dob, string email)
        {
            if (HoloNETManager.Instance.HoloNETEntriesShared == null || (HoloNETManager.Instance.HoloNETEntriesShared != null && !HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].IsInitialized))
                HoloNETManager.Instance.InitHoloNETCollection(client);

            else if (HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].HoloNETClient.EndPoint != client.EndPoint)
                HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].HoloNETClient = client;

            if (HoloNETManager.Instance.HoloNETEntriesShared != null && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].IsInitialized)
            {
                HoloNETManager.Instance.ShowStatusMessage($"Adding HoloNET Entry To Collection...", StatusMessageType.Information, true, HoloNETEntryUIManager.CurrentHoloNETEntryUI);
                HoloNETManager.Instance.LogMessage($"APP: Adding HoloNET Entry To Collection...");

                //Non-async way (you need to use the OnCollectionLoaded event to know when the collection has loaded and receive the data/collection.
                //For non-async methods you will need to wait till the OnInitialized event to raise before calling any other methods such as the one below...
                //HoloNETEntries.AddHoloNETEntryToCollectionAndSave(new AvatarMultiple()
                //{
                //    FirstName = firstName,
                //    LastName = lastName,
                //    DOB = new DateTime(Convert.ToInt32(parts[2]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[0])),
                //    Email = email
                //});

                //Will add the entry to the collection and then persist the change to the hc/rust/happ code.
                //We don't need to call Save() on the entry before calling this method because this method will automatically save the entry and then add it to the collection. It can also of course add an existing entry to the collection. The same applies to the SaveCollection method below.
                ZomeFunctionCallBackEventArgs result = await HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].AddHoloNETEntryToCollectionAndSaveAsync(new AvatarShared()
                {
                    FirstName = firstName,
                    LastName = lastName,
                    DOB = dob, //new DateTime(Convert.ToInt32(parts[2]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[0])),
                    Email = email
                });

                HandleHoloNETEntryAddedToCollection(result);

                //Allows you to batch add/remove multiple entries to the collection and then persist the changes to the hc/rust/happ code in one go.
                //Will only add the entry to the collection in memory (it will NOT persist to hc/rust/happ code until SaveCollection is called.
                //HoloNETEntries.Add(new AvatarMultiple()
                //{
                //    FirstName = firstName,
                //    LastName = lastName,
                //    DOB = new DateTime(Convert.ToInt32(parts[2]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[0])),
                //    Email = email
                //});

                //Will look for any changes since the last time this method was called (includes entries added/removed from the collection as well as any changes made to entries themselves). This can invoke multiple events including OnHoloNETEntryAddedToCollection, OnHoloNETEntryRemovedFromCollection & OnHoloNETEntriesUpdated (if any changes were made to the entries themselves))/
                //HoloNETEntries.SaveCollection(); 
            }
        }

        private void HandleHoloNETCollectionLoaded(HoloNETCollectionLoadedResult<AvatarShared> result)
        {
            //TODO: TEMP UNTIL REAL DATA IS RETURNED! REMOVE AFTER!
            if (HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name] != null && HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Count == 0)
            {
                if (HoloNETManager.Instance.CurrentApp.Name == HoloNETManager.Instance.HoloNETCollectionDemoAppId)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        AvatarShared avatar1 = new AvatarShared()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "David",
                            LastName = "Ellams",
                            Email = "davidellams@hotmail.com",
                            DOB = new DateTime(1980, 4, 11)
                        };

                        //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                        avatar1.MockData();
                        HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar1);
                    }

                    AvatarShared avatar = new AvatarShared()
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "James",
                        LastName = "Ellams",
                        Email = "davidellams@hotmail.com",
                        DOB = new DateTime(1980, 4, 11)
                    };

                    //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                    avatar.MockData();
                    HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);

                    avatar = new AvatarShared()
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Noah",
                        LastName = "Ellams",
                        Email = "davidellams@hotmail.com",
                        DOB = new DateTime(1980, 4, 11)
                    };

                    //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                    avatar.MockData();
                    HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        AvatarShared avatar = new AvatarShared()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "Elba",
                            LastName = "Ellams",
                            Email = "davidellams@hotmail.com",
                            DOB = new DateTime(1980, 4, 11)
                        };

                        //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                        avatar.MockData();
                        HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);

                        avatar = new AvatarShared()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "David",
                            LastName = "Ellams",
                            Email = "davidellams@hotmail.com",
                            DOB = new DateTime(1980, 4, 11)
                        };

                        //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                        avatar.MockData();
                        HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);

                        avatar = new AvatarShared()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "James",
                            LastName = "Ellams",
                            Email = "davidellams@hotmail.com",
                            DOB = new DateTime(1980, 4, 11)
                        };

                        //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                        avatar.MockData();
                        HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);

                        avatar = new AvatarShared()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "Noah",
                            LastName = "Ellams",
                            Email = "davidellams@hotmail.com",
                            DOB = new DateTime(1980, 4, 11)
                        };

                        //TODO: REMOVE AFTER, TEMP TILL GET ZOMECALLS WORKING AGAIN!
                        avatar.MockData();
                        HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name].Add(avatar);
                    }
                }
            }

            RefreshEntriesGrid();

            if (!result.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Loaded.", StatusMessageType.Error);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Loaded.");

                //gridDataEntries.ItemsSource = result.Entries; //Can set it using this or line below.
                //gridDataEntries.ItemsSource = HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name];
                RefreshEntriesGrid();
            }
            else
            {
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.ShowStatusMessage(result.Message, StatusMessageType.Error);
                HoloNETManager.Instance.ShowStatusMessage($"Error Occured Loading HoloNET Collection.", StatusMessageType.Error);
                HoloNETManager.Instance.LogMessage($"APP: Error Occured Loading HoloNET Collection. Reason: {result.Message}");
            }
        }

        private void HandleHoloNETEntryAddedToCollection(ZomeFunctionCallBackEventArgs result)
        {
            if (result.IsCallSuccessful && !result.IsError)
            {
                RefreshEntriesGrid();

                HoloNETEntryUIManager.CurrentHoloNETEntryUI.FirstName = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.LastName = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.DOB = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.Email = "";

                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Entry Added To Collection.", StatusMessageType.Success, false, HoloNETEntryUIManager.CurrentHoloNETEntryUI);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Entry Added To Collection.");
            }
            else
            {
                //TODO: TEMP! REMOVE AFTER!
                RefreshEntriesGrid();

                //TODO: TEMP! REMOVE AFTER!
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.FirstName = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.LastName = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.DOB = "";
                HoloNETEntryUIManager.CurrentHoloNETEntryUI.Email = "";

                HoloNETEntryUIManager.CurrentHoloNETEntryUI.ShowStatusMessage(result.Message, StatusMessageType.Error);
                HoloNETManager.Instance.ShowStatusMessage($"Error Occured Adding HoloNET Entry To Collection.", StatusMessageType.Error);
                HoloNETManager.Instance.LogMessage($"APP: Error Occured Adding HoloNET Entry To Collection. Reason: {result.Message}");
            }
        }

        private void RefreshEntriesGrid()
        {
            ucHoloNETEntryAndCollectionSharedPopup ucHoloNETEntryAndCollectionSharedPopup = PopupManager.CurrentPopup as ucHoloNETEntryAndCollectionSharedPopup;

            if (ucHoloNETEntryAndCollectionSharedPopup != null)
                ucHoloNETEntryAndCollectionSharedPopup.gridDataEntries.ItemsSource = HoloNETManager.Instance.HoloNETEntriesShared[HoloNETManager.Instance.CurrentApp.Name];
        }

        private void HoloNETEntriesShared_OnInitialized(object sender, ReadyForZomeCallsEventArgs e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Initialized", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Initialized.");
            }
        }

        private void HoloNETEntriesShared_OnCollectionLoaded(object sender, HoloNETCollectionLoadedResult<AvatarShared> e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Loaded", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Loaded: {HoloNETManager.Instance.GetEntryInfo(e.ZomeFunctionCallBackEventArgs)}");
            }
        }

        private void HoloNETEntriesShared_OnCollectionSaved(object sender, HoloNETCollectionSavedResult e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Data Entries Updated (Collection Updated)", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Data Entries Updated (Collection Updated).");
            }
        }

        private void HoloNETEntriesShared_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string msg = $"HoloNET Collection Changed. Action: {Enum.GetName(typeof(NotifyCollectionChangedAction), e.Action)}, New Items: {(e.NewItems != null ? e.NewItems.Count : 0)}, Old Items: {(e.OldItems != null ? e.OldItems.Count : 0)}";
            HoloNETManager.Instance.LogMessage($"APP: {msg}");
            HoloNETManager.Instance.ShowStatusMessage(msg, StatusMessageType.Information, true);
        }

        private void HoloNETEntriesShared_OnHoloNETEntryAddedToCollection(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Data Entry Added To Collection", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Data Entry Added To Collection: {HoloNETManager.Instance.GetEntryInfo(e)}");
            }
        }

        private void HoloNETEntriesShared_OnHoloNETEntryRemovedFromCollection(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Data Entry Removed From Collection", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Data Entry Removed From Collection: {HoloNETManager.Instance.GetEntryInfo(e)}");
            }
        }

        private void HoloNETEntriesShared_OnClosed(object sender, HoloNETShutdownEventArgs e)
        {
            if (!e.IsError)
            {
                HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Closed", StatusMessageType.Success);
                HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Closed.");
            }
        }

        private void HoloNETEntriesShared_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HoloNETManager.Instance.ShowStatusMessage($"HoloNET Collection Error", StatusMessageType.Error);
            HoloNETManager.Instance.LogMessage($"APP: HoloNET Collection Error: {e.Reason}");
        }
    }
}