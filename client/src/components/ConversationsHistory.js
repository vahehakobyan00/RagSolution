import { Button, Typography } from "antd";
import { HistoryOutlined } from "@ant-design/icons";
import React from "react";

export const ConversationsHistory = ({
                                         collapsed,
                                         historyData,
                                         getConversationById,
                                         activeConversationId,
                                         setActiveConversationId,
                                         streaming
                                     }) => {
    return (
        <div className={'chat-history'}>
            <div className="chat-history-wrapper">
                <span className={'history'}>History</span>
                <div className="chat-history-scroll">
                    {Array.isArray(historyData) &&
                        historyData.map((item) => (
                            <Button
                                disabled={streaming}
                                key={item.id}
                                onClick={() => {
                                    setActiveConversationId(item.id);
                                    getConversationById(item.id);
                                }}
                                className={`curs-pointer history-item ${
                                    item.id === activeConversationId ? "active" : ""
                                } ${collapsed ? "collapsed" : ""}`}
                            >
                                <Typography.Paragraph
                                    className="history-item-text"
                                    ellipsis={{rows: 1}}
                                >
                                    {collapsed ? <HistoryOutlined/> : item.title}
                                </Typography.Paragraph>
                            </Button>
                        ))}
                </div>
            </div>
        </div>
    );
};
