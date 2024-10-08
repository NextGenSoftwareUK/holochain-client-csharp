﻿# HoloNET Test Harness

Test Harness for HoloNET Holochain Client.

https://github.com/holochain-open-dev/holochain-client-csharp \
https://github.com/NextGenSoftwareUK/holochain-client-csharp

You need to clone the following repo:
https://github.com/holochain/happ-build-tutorial

And follow the instructions here:
https://github.com/holochain-open-dev/wiki/wiki/Installing-Holochain--&-Building-hApps-Natively-On-Windows

Once you have Holochain setup on your machine and got the example hApp ready above, you need to copy it into a hApps folder in the root of the output folder (Debug or Release) where you installed this Test Harness package. HoloNET will be looking for it there.

The Test Harness sets the paths to the test hApp you compiled above using the following lines:

````c#
_holoNETClientAppAgent.Config.FullPathToRootHappFolder = string.Concat(Environment.CurrentDirectory, @"\hApps\happ-build-tutorial-develop");
_holoNETClientAppAgent.Config.FullPathToCompiledHappFolder = string.Concat(Environment.CurrentDirectory, @"\hApps\happ-build-tutorial-develop\workdir\happ");
````

If you wish to run the OASIS test (real world use case saving and loading an entry) then you will need to clone this repo: \
https://github.com/NextGenSoftwareUK/OASIS-Holochain-hApp

The Test Harness will set the paths to the OASIS hApp you compiled above using the following lines:

````c#
_holoNETClientAppAgent.Config.FullPathToRootHappFolder = string.Concat(Environment.CurrentDirectory, @"\hApps\OASIS-Holochain-hApp");
_holoNETClientAppAgent.Config.FullPathToCompiledHappFolder = string.Concat(Environment.CurrentDirectory, @"\hApps\OASIS-Holochain-hApp\zomes\workdir\happ");
````

Finally, from within your app simply call the following method:

````c#
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;

await HoloNETTestHarness.TestHoloNETClientAsync(TestToRun testToRun);
````

You can pass in one of these values:

| Test														  | Description                                                                                            
|-------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| WhoAmI													  | Will call zome function whoami on the whoami zome (part of the happ-build-tutorial above).																																																																								  |
| Numbers													  | Will call zome function add_ten on the numbers zome (part of the happ-build-tutorial above).																																																																							  |
| Signal													  | Will call zome function test_signal_as_string on the oasis zome.																																																																														  |
| SaveLoadOASISEntryWithTypeOfEntryDataObject				  | Will call the zome function create_entry_avatar on the oasis zome and once it receives the ActionHash back from the Holochain Conductor, it will load that entry using get_entry_avatar zome function (part of the OASIS hApp above). It will then map the data returned onto a new instance of the type passed in to CallZomeFunction function. See main HoloNET README for more info... |
| SaveLoadOASISEntryWithEntryDataObject						  | Will call the zome function create_entry_avatar on the oasis zome and once it receives the ActionHash back from the Holochain Conductor, it will load that entry using get_entry_avatar zome function (part of the OASIS hApp above). It will then map the data returned onto the object passed in to CallZomeFunction function. See main HoloNET README for more info...    			  |
| SaveLoadOASISEntryUsingSingleHoloNETAuditEntryBaseClass     | Will test the new HoloNETAuditEntryBaseClass by extending it with the Avatar class. It will call create_entry_avatar, get_entry_avatar, update_entry_avatar & delete_entry_avatar on the oasis zome and verify the results are as expected. This version will instantiate its own internal HoloNET Client to make calls to the Holochain Conductor.										  |
| SaveLoadOASISEntryUsingMultipleHoloNETAuditEntryBaseClasses | Will test the new HoloNETAuditEntryBaseClass by extending it with the AvatarMultiple & Holon class. It will call create_entry_avatar, get_entry_avatar, update_entry_avatar & delete_entry_avatar on the oasis zome and verify the results are as expected. This version passes in a HoloNETClient instance that is shared between the AvatarMultiple & Holon classes.					  |
| LoadTestNumbers											  | Will call zome function add_ten on the numbers zome 100 times (part of the happ-build-tutorial above).																																																																					  |
| LoadTestSaveLoadOASISEntry								  | Will call SaveLoadOASISEntry test 100 times (part of the OASIS happ above).																																																																												  |

You can also view the full source and run the Test Harness (and edit to suit your needs etc) here: \
https://github.com/holochain-open-dev/holochain-client-csharp/tree/main/NextGenSoftware.Holochain.HoloNET.Client.TestHarness

**NuGet Package:** \
https://www.nuget.org/packages/NextGenSoftware.Holochain.HoloNET.Client.TestHarness
