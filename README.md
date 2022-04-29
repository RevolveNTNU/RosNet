<div class="row">
  <div class="column">

  </div>
  <div class="column">

  </div>
</div>
<p float="left">
    <a href="https://revolve.no/"><img align=left src="https://raw.githubusercontent.com/RevolveNTNU/RosNet/86-improve-readme/.github/main/revolve.svg" width="30%"/></a>
    <a href="https://ros.org/"><img align=right margin src="https://raw.githubusercontent.com/RevolveNTNU/RosNet/86-improve-readme/.github/main/ros.png" width="30%"/></a>
</p>
<br>
<br>
<br>
<br>


# RosNet
## What is RosNet?
RosNet is a .NET library used for parsing RosBags to a C# object. We are thinking of adding support for writing as well. 

## How to use the library:
<!-- How to include library in project-->
```C#
using RosNet.DataModel;

# Convert a ROSBag to a C# object
var rosBag = new RosBag(path);
rosBag.Read();

# Obtain possible ROS topics and fields in the topics
Dictionary<string, List<string>> fieldNamesByTopic = rosBag.GetConnectionFields();

# Obtain list of tuples with timestamp and fieldvalue
List<(Time, FieldValue)> timeSeries = rosBag.GetTimeSeries(topic, fieldName);
```

## External dependencies
SharpZipLib used for decompression from bz2: http://icsharpcode.github.io/SharpZipLib/

## Credits
This library is made by the organization Revolve NTNU: https://www.revolve.no

Contributors:  
Henrik Hørlück Berg https://github.com/henrikhorluck  
Inge Grelland https://github.com/Kytzis  
Juni Sæther Skarpaas https://github.com/Juni-hub  
Lars van der Lee https://github.com/TheLarsinator  
Mikael Steenbuch https://github.com/mikaelste

<!-- License -->
