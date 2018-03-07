# GroupChatApplication
Have made no changes in code.

This application divided in three logical units.
  1. Chat service and its logic implementation.
  2. Server which host service
      Server has to run on windows elevated mode.
      One can specify server ip and port. Usually it should left as 127.0.0.1 or one can say localhost
      Where in port should be specified by user.
      If one is running server behind firewall and try to access server from other network. Please check server port forwarding.
      For communication I have used TCP/IP protocol. 
  3. Chat client which will be running on client machine.
      a. This is simple chat client application. Which is as of now can plain Text messages asynchronously over TCP/IP.
      b. Just to show demo that it is responsive application and use asynchronous mode for communication. I have provided loop button. 
         which will send text message on interval of 2 sec continuously for 50 messages.
      c. I have also provided cancel mechanism so one can terminate background worker.
      d. Connect and disconnect functionality is put on single thread so application will be freeze while connecting and disconnecting.
      
  
