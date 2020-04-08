
% Put the three (3) dll's in the current directory for the task!
warning ('off', 'MATLAB:NET:AddAssembly:nameConflict');
NET.addAssembly([pwd '\EventExchanger.dll']);
warning ('on', 'MATLAB:NET:AddAssembly:nameConflict');

% the needed dll are: 
% EventExchanger.dll, 
% HidSharp.dll and
% HidSharp.DeviceHelpers.dll

% Create and start the object
EE = ID.EventExchanger;
EE.Start();

% if multiple EVT-2s are connected add a serialnumber as parameter to start
% e.g., 
% EE.Start('11007');

% to pulse the port with number x for y millisecs do:
% EE.PulseEvent(x,y);

EE.PulseEvent(255,4);

% Thank you, come again!

