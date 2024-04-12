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




   


