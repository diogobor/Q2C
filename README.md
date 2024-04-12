# Q2C
Software responsible for managing the list of waiting samples in mass spectrometer

# Equipment
## Hardware
- A computer with a minimum of 16 GB RAM and 4 computing cores is recommended.  However, the software can take advantage of superior configurations.

## Software
-	Windows 10 (64 bits) or later.
-	The .NET Core 7 or later.
-	The Q2C software, available for download at https://github.com/diogobor/Q2C/releases

# Procedures

1. **Software installation:**<br/>
  1.1 Download Q2C by clicking on <i>Q2C_setup_64bit.msi</i> in the [latest release](https://github.com/diogobor/Q2C/releases/).
  <br/>1.2 Install it by double-clicking the previous downloaded file.

1. **Workflow:**<br/>
  2.1 <i>Set up a Google cloud project:</i><br/>
    &emsp;2.1.1 Go to [Google account](https://accounts.google.com) and sign up or log in.<br/>
    &emsp;2.1.2 Go to [Google cloud platform](https://console.cloud.google.com/) and create a new project.<br/>
    &emsp;&emsp;2.1.2.1 To do so, go to <i>Applications</i>. (<b>Figure 1</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/9ae30cf2-cdd1-4ce9-a3fe-b01ff95d3dcf"><br/>
   <b>Figure 1: Access Google cloud platform.</b></p>
    &emsp;&emsp;2.1.2.2 Click on '<i>New Project</i>'. (<b>Figure 2</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/f4cb4f03-1990-4b9c-a945-1f3f9cb8bfd7"><br/>
   <b>Figure 2: Create a new project.</b></p>
     &emsp;&emsp;2.1.2.3 Set a project name (<i>q2c-software</i>) and click on '<i>Create</i>' button. (<b>Figure 3</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/668b43d7-7020-42e9-851a-a98c645a3393"><br/>
   <b>Figure 3: Set a project name.</b></p>
   &emsp;2.1.3 Go back to <i>Applications</i> (<b>Figure 1</b>) and click on <i>q2c-software</i> to set it up. (<b>Figure 4</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/ccc57af1-df49-4ea0-8194-d96e032cf506"><br/>
   <b>Figure 4: Set a project up.</b></p>
   &emsp;2.1.4 On the left side of the <i>Google Cloud icon</i>, click on the menu to display the options. Then, go to <i>APIs and services → OAuth consent screen</i> (<b>Figure 5</b>) and go to Create an <i>External User Type</i>. (<b>Figure 6</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/4eb6f127-10b3-4065-8da0-62a65944de94"><br/>
   <b>Figure 5: Go to <i>APIs and services → OAuth consent screen</i>.</b></p>
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/bf29fa3e-662b-456e-95f6-f9ac450d0703"><br/>
   <b>Figure 6: Create an external user type.</b></p>
   &emsp;&emsp;2.1.4.1 Set up an app name (<i>q2c-software</i>) and select a valid Gmail. (<b>Figure 7</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/881e3f35-7043-4690-ba2f-a841fee68e1d"><br/>
   <b>Figure 7: Start to set up an external user type by giving an app name and selecting a valid Gmail.</b></p>
   &emsp;&emsp;2.1.4.2 Set an email for the developer contact information (<i>set the same used in the previous setp</i>) and click on the '<i>Save and Continue</i>' button. (<b>Figure 8</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/b0d9d02b-e581-4025-8ffa-81d19e29250d"><br/>
   <b>Figure 8: Save the external user type.</b></p>
   &emsp;&emsp;2.1.4.3 Click on '<i>Save and Continue</i>' button in <i>Scopes</i> and <i>Test users</i> steps. Finally, in '<i>Summary</i>' step, go to the bottom and click on the '<i>Back to dashboard</i>' button.<br/><br/>
   &emsp;2.1.5 Go to <i>Credentials</i>, click on <i>Create Credentials</i>, then on <i>OAuth client ID</i>. (<b>Figure 9</b>)
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/d3c156e5-a667-4ea8-a582-ac2a71a86e20"><br/>
   <b>Figure 9: Create credentials.</b></p>
   &emsp;&emsp;2.1.5.1 In <i>Application type</i>, select <i>Desktop app</i> (<b>Figure 10a</b>), then type a name (<i>q2c-software</i>) and click on <i>Create</i> button. (<b>Figure 10b</b>)
   <p align="center">
     <img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/4877afec-b450-4f7e-9f80-e5d49dcc69ee">&emsp;<img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/6a337679-8adf-4d27-a272-13409a69909e"><br/>
   <b>Figure 10: Create OAuth client ID.</b></p>
   &emsp;&emsp;2.1.5.2 Once the OAuth client ID is created, a new window is displayed with the <b>Google Client ID</b> and <b>Google Secret ID</b>. (<b>Figure 11</b>) Both IDs will be used to set up the database on Q2C.
   <p align="center"><img width="35%" alt="image" src="https://github.com/diogobor/Q2C/assets/7681148/d52d6ec7-1967-4e1b-94b9-dfba37cdf71e"><br/>
   <b>Figure 11: Client and Secret IDs. Both of them are used to set up the database on Q2C.</b></p>


   


