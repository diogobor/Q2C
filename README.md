# Q2C

We present Q2C, an open-source software designed to streamline mass spectrometer queue management and assess performance based on quality control metrics.<br/>Q2C provides a fast and user-friendly interface to visualize projects queues, manage analysis schedules and keep track of samples that were already processed. Our software includes analytical tools to ensure equipment calibration and provides comprehensive log documentation for machine maintenance, enhancing operational efficien-cy and reliability.<br/>Additionally, Q2C integrates with Google® Cloud, allowing users to access and manage the software from different locations while keeping all data synchronized and seamlessly integrated across the system.

Data are available from the ProteomeXchange consortium (identifier [PXD055186](https://www.ebi.ac.uk/pride/archive/projects/PXD055186)).

# Equipment
## Hardware
- A computer with a minimum of 16 GB RAM and 4 computing cores is recommended.  However, the software can take advantage of superior configurations.

## Software
-	Windows 10 (64 bits) or later.
-	The .NET Core 7 or later.
-	For reading Thermo® RAW files, the MSFileReader must be installed. To do so, create an account on [Thermo®](https://thermo.flexnetoperations.com/control/thmo/login), register, choose <i>Other Releases → MSFileReader 3.1 SP4</i> and download <i>MSFileReader_x64.exe</i>.
-	The Q2C software, available for download at https://github.com/diogobor/Q2C/releases
-	A Google® account

## Data files
-	Q2C is compatible with data files in the formats mzML (proposed by [HUPO Proteomics Standard Initiative](http://www.psidev.info/)) and Thermo® RAW files.

# Procedures

<div id="ref_1">1. <b>Software installation:</b></div>
There are two modes to run Q2C: online and offline. To setup the first one, start from 1.1, otherwise, go to <a href="#ref_1_2">1.2</a>.<br/><br/>
1.1 <i>Set up a Google cloud project:</i><br/>
    &emsp;<i>Note: If the Google cloud project has already been established, proceed to step <a href="#ref_1_2">1.2</a>.</i><br/><br/>
    <div id="ref_1_1_1">&emsp;1.1.1 Go to <a href="https://accounts.google.com" target="_">Google account</a> and sign up or log in.</div>
    &emsp;1.1.2 Go to <a href="https://console.cloud.google.com" target="_">Google cloud platform</a> and create a new project.<br/>
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
<div id="ref_1_2">&emsp;1.2 Download Q2C by clicking on <i>Q2C_setup_64bit.msi</i> in the <a href="https://github.com/diogobor/Q2C/releases/" target="_">latest release</a>.</div>
&emsp;1.3 Install it by double-clicking the previous downloaded file.<br/><br/>

<div id="ref_2">2. <b>Workflow:</b></div>
  &emsp;2.1 <i>Set up Q2C:</i><br/>
  &emsp;&emsp;2.1.1 As mentioned in <a href="#ref_1">Software installation</a> section, Q2C can be run either online or offline. To setup the offline mode, on the <i>Database settings</i> screen, check <i>Offline Mode</i> option.<br/>
  <div id="ref_2_1_1_1">&emsp;&emsp;&emsp;2.1.1.1 Click on the '<i>Confirm</i>' button (<b>Figure 17a</b>), and click on '<i>Yes</i>' to accept the modified entries. (<b>Figure 17b</b>)</div>
  &emsp;&emsp;&emsp;2.1.1.2 Go to <a href="#ref_2_1_3">2.1.3</a>.<br/><br/>
  <p align="center">
  <img width="30%" alt="image" src="https://github.com/user-attachments/assets/baad7312-e4ea-455c-8f0a-5d219f41efe8">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/7b05461e-3d0b-4ce2-ad9b-93c0b61bf816"><br/>
   <b>Figure 17: Set up the database.</b></p>
  &emsp;&emsp;2.1.2 On the other hand, the online mode requires <b>Google Client ID</b> and <b>Google Client Secret</b> according to the IDs obtained in <a href="#ref_1_1_5_2">1.1.5.2</a> section.<br/>
  &emsp;&emsp;&emsp;2.1.2.1 If you are not the administrator, uncheck the <i>Create spreadsheet</i> option, and set the <b>Spreadsheet ID</b> obtained when the administrator set up the Q2C for the first time.<br/>
  &emsp;&emsp;&emsp;2.1.2.2 Click on the '<i>Confirm</i>' button. Similar to <a href="#ref_2_1_1_1">2.1.1.1</a>.<br/>
  &emsp;&emsp;&emsp;2.1.2.3 Q2C will be redirected to Google login page. Type the created email in <a href="#ref_1_1_1">1.1.1</a> section, click on the '<i>Next</i>' button and enter the password.  (<b>Figure 18</b>)<br/>
<i>PS1: Make sure the message 'Sign in to continue to <b>q2c-software</b>' is displayed.</i><br/>
<i>PS2: You have 90 seconds to complete this step.</i><br/>
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/e406e0e4-f25f-4dc6-ab52-f7fce5017061"><br/>
<b>Figure 18: Enter the credentials.</b></p>
&emsp;&emsp;&emsp;2.1.2.4 Give Google permission to access the app by clicking on '<i>Advanced</i>' link (<b>Figure 19a</b>). Then, click on the '<i>Go to q2c-software (unsafe)</i>' link. (<b>Figure 19b</b>)
<p align="center">
  <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/7fa879b0-d974-4941-b1ac-66f88d2b9ba2">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/a49e0d17-48ee-4fea-990e-cc8f7a3aaa26"><br/>
   <b>Figure 19: Give Google permission to access Q2C.</b></p>
&emsp;&emsp;&emsp;2.1.2.5 On the next page, '<i>q2c-software wants access to your Google Account</i>', check the '<i>Select all</i>' option, then click on the '<i>Continue</i>' button. (<b>Figure 20</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/e8737b55-c231-4e03-9762-efdb3868ff22"><br/>
<b>Figure 20: Select all option to give Google permission.</b></p>
&emsp;&emsp;&emsp;2.1.2.6 Once the permission is done, the next page will display the following message '<i>Received verification code. You may now close this window</i>'. Go back to Q2C. (<b>Figure 21</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/3631e2f3-eac3-434b-bf66-8e962a6625ca"><br/>
<b>Figure 21: Google permission is complete.</b></p>
<div id="ref_2_1_3">&emsp;&emsp;2.1.3 Q2C will display a new window to <b>add new users</b>. Click on '<i>Add User</i>' button. (<b>Figure 22</b>)</div>
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/dba6e40c-2c34-49df-84e5-f5f9a3db6b8d"><br/>
<b>Figure 22: List of registered users.</b></p>
&emsp;&emsp;&emsp;2.1.3.1 To add a new user, enter a username (<i>must be the same as the Windows® username.</i>), a valid email and set a category. Then, click on '<i>Confirm</i>' button. Q2C will be restarted. (<i>PS: the first registered user is configured as Administrator.</i>) (<b>Figure 23</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/3c1689a4-7aa2-40f8-a2b1-49791148301f"><br/>
<b>Figure 23: Add new user.</b></p>
&emsp;&emsp;&emsp;&emsp;2.1.3.1.1 There are 8 different types of category:<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.1 <i>User</i>: the basic category. Only allows the user to view the project queue (section 2.2).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.2 <i>User Sample</i>: Allows the user to add/edit/remove projects to the queue (section 2.2).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.3 <i>Super User Sample</i>: Allows the user to add/edit/remove projects to the queue. In addition, it also allows the user to view the runs (section 4).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.4 <i>Super User Machine</i>: Allows the user to add/edit/remove runs (section 2.3).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.5 <i>Super User Sample & Machine</i>: Allows the user to add/edit/remove projects and runs (sections 2.2 & 2.3).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.6 <i>Master User Sample</i>: Allows the user to add/edit/remove projects and put them in the machine queue (sections 2.2).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.7 <i>Master User Sample & Machine</i>: Allows the user to add/edit/remove projects and runs and put projects in the machine queue (sections 2.2 & 2.3).<br/>
&emsp;&emsp;&emsp;&emsp;&emsp;2.1.3.1.1.8 <i>Administrator</i>: Allows the user to add/edit/remove databases, users and machines. In addition to having all the <i>Master User Samples & Machine</i> functions (sections 2.2 & 2.3).<br/><br/>
&emsp;&emsp;2.1.4 Q2C will display a new window to <b>add new machines</b>. Click on '<i>Add Machine</i>' button. (<b>Figure 24</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/69b4e458-4fd9-41a3-920d-7a7d30d6824b"><br/>
<b>Figure 24: List of registered machines.</b></p>
&emsp;&emsp;&emsp;2.1.4.1 To add a new machine, enter a name, set the frequency of the <i>mass calibration time</i> (<i>default is 2 weeks</i>), set the frequency of the <i>full calibration time</i> (<i>default is 1 month</i>), set the <i>interval time (in minutes)</i> for each project (<i>default is 20</i>), and check the options that satisfy the machine: <i>Evaluation, FAIMS, OT & IT</i>. Then, click on '<i>Confirm</i>' button. Q2C will be restarted. (<b>Figure 25</b>)
<p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/b7fb01d1-3229-4038-babb-a70386119075"><br/>
<b>Figure 25: Add new machine.</b></p>
2.2 <b>Projects:</b><br/>
<p>The main Q2C interface shows all registered projects. (Figure 26)</p>
<p align="center"><img width="60%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/7091665e-e861-4119-8ff7-96260b274c24"><br/>
<b>Figure 26: Graphical User Interface of Q2C’s main window.</b></p>
<i>Watch this tutorial video, which explains how Q2C works.</i>


https://github.com/diogobor/Q2C/assets/7681148/1e6f5e72-abfd-4584-ae1b-d2682a5e716c


