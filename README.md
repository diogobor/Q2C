# Q2C
Software responsible for:
- managing the list of waiting samples in mass spectrometer(s)
- evaluating the mass spectrometer(s) performance

# Equipment
## Hardware
- A computer with a minimum of 16 GB RAM and 4 computing cores is recommended.  However, the software can take advantage of superior configurations.

## Software
-	Windows 10 (64 bits) or later.
-	The .NET Core 7 or later.
-	The Q2C software, available for download at https://github.com/diogobor/Q2C/releases
-	A Google® Gmail account

# Procedures

1. **Software installation:**<br/>
1.1 <i>Set up a Google cloud project:</i><br/>
    <div id="ref_1_1_1">&emsp;1.1.1 Go to <a href="https://accounts.google.com">Google account</a> and sign up or log in.</div>
    &emsp;1.1.2 Go to <a href="https://console.cloud.google.com">Google cloud platform</a> and create a new project.<br/>
    &emsp;&emsp;1.1.2.1 To do so, go to <i>Select a project</i>. (<b>Figure 1</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/f1939a67-fedc-41e0-830e-f6b2bedf7066"><br/>
   <b>Figure 1: Access Google cloud platform.</b></p>
    &emsp;&emsp;1.1.2.2 Click on '<i>New Project</i>'. (<b>Figure 2</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/f4cb4f03-1990-4b9c-a945-1f3f9cb8bfd7"><br/>
   <b>Figure 2: Create a new project.</b></p>
     &emsp;&emsp;1.1.2.3 Set a project name (<i>q2c-software</i>) and click on '<i>Create</i>' button. (<b>Figure 3</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/668b43d7-7020-42e9-851a-a98c645a3393"><br/>
   <b>Figure 3: Set a project name.</b></p>
   &emsp;1.1.3 Go back to <i>Applications</i> (<b>Figure 1</b>) and click on <i>q2c-software</i> to set it up. (<b>Figure 4</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/ccc57af1-df49-4ea0-8194-d96e032cf506"><br/>
   <b>Figure 4: Set a project up.</b></p>
   &emsp;1.1.4 On the left side of the <i>Google Cloud icon</i>, click on the menu to display the options. Then, go to <i>APIs and services → OAuth consent screen</i> (<b>Figure 5</b>) and go to Create an <i>External User Type</i>. (<b>Figure 6</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/4eb6f127-10b3-4065-8da0-62a65944de94"><br/>
   <b>Figure 5: Go to <i>APIs and services → OAuth consent screen</i>.</b></p>
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/bf29fa3e-662b-456e-95f6-f9ac450d0703"><br/>
   <b>Figure 6: Create an external user type.</b></p>
   &emsp;&emsp;1.1.4.1 Set up an app name (<i>q2c-software</i>) and select a valid Gmail. (<b>Figure 7</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/881e3f35-7043-4690-ba2f-a841fee68e1d"><br/>
   <b>Figure 7: Start to set up an external user type by giving an app name and selecting a valid Gmail.</b></p>
   &emsp;&emsp;1.1.4.2 Set an email for the developer contact information (<i>set the same used in the previous setp</i>) and click on the '<i>Save and Continue</i>' button. (<b>Figure 8</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/b0d9d02b-e581-4025-8ffa-81d19e29250d"><br/>
   <b>Figure 8: Save the external user type.</b></p>
   &emsp;&emsp;1.1.4.3 Click on '<i>Save and Continue</i>' button in <i>Scopes</i> and <i>Test users</i> steps. Finally, in '<i>Summary</i>' step, go to the bottom and click on the '<i>Back to dashboard</i>' button.<br/><br/>
   &emsp;&emsp;1.1.4.4 On the dashboard, click on the '<i>Publish App</i>' button (<b>Figure 9a</b>) and confirm the publication. (<b>Figure 9b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/8fe22ebe-d24c-4583-8879-1cc262f6d96b">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/481bdc40-e8b9-4241-979c-7466b640dcaa"><br/>
   <b>Figure 9: Publish <i>q2c-software</i> app.</b></p>
   &emsp;1.1.5 Go to <i>Credentials</i>, click on <i>Create Credentials</i>, then on <i>OAuth client ID</i>. (<b>Figure 10</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/d3c156e5-a667-4ea8-a582-ac2a71a86e20"><br/>
   <b>Figure 10: Create credentials.</b></p>
   &emsp;&emsp;1.1.5.1 In <i>Application type</i>, select <i>Desktop app</i> (<b>Figure 11a</b>), then type a name (<i>q2c-software</i>) and click on <i>Create</i> button. (<b>Figure 11b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/4877afec-b450-4f7e-9f80-e5d49dcc69ee">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/6a337679-8adf-4d27-a272-13409a69909e"><br/>
   <b>Figure 11: Create OAuth client ID.</b></p>
   <div id="ref_1_1_5_2">&emsp;&emsp;1.1.5.2 Once the OAuth client ID is created, a new window is displayed with the <b>Google Client ID</b> and <b>Google Secret ID</b>. (<b>Figure 12</b>) Both IDs will be used to set up the database on Q2C.</div>
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/d52d6ec7-1967-4e1b-94b9-dfba37cdf71e"><br/>
   <b>Figure 12: Client and Secret IDs. Both of them are used to set up the database on Q2C.</b></p>
   &emsp;1.1.6 Go to <i>Enabled APIs and services</i> and click on <i>Enable APIs and Services</i>. (<b>Figure 13</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/cbaaee51-370b-49f0-b9a9-55496448663e"><br/>
   <b>Figure 13: Enable APIs and Services.</b></p>
   &emsp;&emsp;1.1.6.1 In the API Library, search for <i>google drive api</i> (<b>Figure 14a</b>) and enable it. (<b>Figure 14b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/2437717c-675e-4610-86cd-d2565f036a9e">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/9812892f-65d7-436a-ada7-d4533a348d17"><br/>
   <b>Figure 14: Enable Google Drive API.</b></p>
   &emsp;&emsp;1.1.6.2 In the API Library, search for <i>gmail api</i> (<b>Figure 15a</b>) and enable it. (<b>Figure 15b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/5969dcc9-2d6d-4b9e-bb07-241bc2104022">&emsp;&emsp;<img width="25%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/481d6378-d198-4bae-9096-cfec58d8a946"><br/>
   <b>Figure 15: Enable Gmail API.</b></p>
   &emsp;&emsp;1.1.6.3 In the API Library, search for <i>google sheets api</i> (<b>Figure 16a</b>) and enable it. (<b>Figure 16b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/32b9fb7a-28f6-4299-95a2-94ad08b08663">&emsp;&emsp;<img width="25%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/616e0468-e7c8-4710-b039-588fcb4a3e87"><br/>
   <b>Figure 16: Enable Google Sheets API.</b></p>
&emsp;1.2 Download Q2C by clicking on <i>Q2C_setup_64bit.msi</i> in the [latest release](https://github.com/diogobor/Q2C/releases/).<br/>
&emsp;1.3 Install it by double-clicking the previous downloaded file.<br/>
&emsp;1.4 Run Q2C.<br/>
&emsp;&emsp;1.4.1 On the <i>Database settings</i> screen, set the <b>Google Client ID</b> and the <b>Google Client Secret</b> according to the IDs obtained in <a href="#ref_1_1_5_2">1.1.5.2</a> section, click on the '<i>Confirm</i>' button (<b>Figure 17a</b>), annd click on '<i>Yes</i>' to accept the modified entries. (<b>Figure 17b</b>)
<p align="center">
  <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/23d46260-2226-46de-aadc-7a4e654e504b">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/7b05461e-3d0b-4ce2-ad9b-93c0b61bf816"><br/>
   <b>Figure 17: Set up the database.</b></p>

&emsp;&emsp;1.4.2 Q2C will be redirected to Google login page. Type the created email in <a href="#ref_1_1_1">1.1.1</a> section, click on the '<i>Next</i>' button and enter the password. <i>Make sure the message 'Sign in to continue to <b>q2c-software</b>' is displayed.</i> (<b>Figure 18</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/e406e0e4-f25f-4dc6-ab52-f7fce5017061"><br/>
<b>Figure 18: Enter the credentials.</b></p>
&emsp;&emsp;1.4.3 Give Google permission to access the app by clicking on '<i>Advanced</i>' link (<b>Figure 19a</b>). Then, click on the '<i>Go to q2c-software (unsafe)</i>' link. (<b>Figure 19b</b>)
<p align="center">
  <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/7fa879b0-d974-4941-b1ac-66f88d2b9ba2">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/a49e0d17-48ee-4fea-990e-cc8f7a3aaa26"><br/>
   <b>Figure 19: Give Google permission to access Q2C.</b></p>
&emsp;&emsp;1.4.4 On the next page, '<i>q2c-software wants access to your Google Account</i>', check the '<i>Select all</i>' option, then click on the '<i>Continue</i>' button. (<b>Figure 20</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/e8737b55-c231-4e03-9762-efdb3868ff22"><br/>
<b>Figure 20: Select all option to give Google permission.</b></p>



2. **Workflow:**<br/>
  2.1 <i>Set up Q2C:</i><br/>
  

